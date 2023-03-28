using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.MIDI;

public sealed class MIDITrackChunk : MIDIChunk
{
	internal const string EXPECTED_NAME = "MTrk";

	public MIDIEvent? First { get; private set; }
	public MIDIEvent? Last { get; private set; }

	/// <summary>Includes the end of track event</summary>
	public int NumEvents { get; private set; }
	public int NumTicks => Last is null ? 0 : Last.Ticks;

	public MIDITrackChunk()
	{
		//
	}
	internal MIDITrackChunk(uint size, EndianBinaryReader r)
	{
		long endOffset = GetEndOffset(r, size);

		int ticks = 0;
		byte runningStatus = 0;
		bool foundEnd = false;
		bool sysexContinue = false;
		while (r.Stream.Position < endOffset)
		{
			if (foundEnd)
			{
				throw new InvalidDataException($"Events found after the {nameof(MetaMessageType.EndOfTrack)} {nameof(MetaMessage)}");
			}

			ReadEvent(r, ref ticks, ref runningStatus, ref foundEnd, ref sysexContinue);
		}
		if (!foundEnd)
		{
			throw new InvalidDataException($"Could not find the {nameof(MetaMessageType.EndOfTrack)} {nameof(MetaMessage)}");
		}

		if (r.Stream.Position > endOffset)
		{
			throw new InvalidDataException("Expected to read a certain amount of events, but the data was read incorrectly...");
		}
	}
	private void ReadEvent(EndianBinaryReader r, ref int ticks, ref byte runningStatus, ref bool foundEnd, ref bool sysexContinue)
	{
		long startOffset = r.Stream.Position;

		ticks += Utils.ReadVariableLength(r);

		// Get command
		byte cmd = r.ReadByte();
		if (sysexContinue && cmd != 0xF7)
		{
			throw new InvalidDataException($"{nameof(SysExContinuationMessage)} was missing at 0x{r.Stream.Position - 1:X}");
		}
		if (cmd < 0x80)
		{
			cmd = runningStatus;
			r.Stream.Position--;
		}

		// Check which message it is
		if (cmd is >= 0x80 and <= 0xEF)
		{
			runningStatus = cmd;
			byte channel = (byte)(cmd & 0xF);
			switch (cmd & ~0xF)
			{
				case 0x80: InsertMessage(ticks, new NoteOffMessage(r, channel)); break;
				case 0x90: InsertMessage(ticks, new NoteOnMessage(r, channel)); break;
				case 0xA0: InsertMessage(ticks, new PolyphonicPressureMessage(r, channel)); break;
				case 0xB0: InsertMessage(ticks, new ControllerMessage(r, channel)); break;
				case 0xC0: InsertMessage(ticks, new ProgramChangeMessage(r, channel)); break;
				case 0xD0: InsertMessage(ticks, new ChannelPressureMessage(r, channel)); break;
				case 0xE0: InsertMessage(ticks, new PitchBendMessage(r, channel)); break;
			}
		}
		else if (cmd == 0xF0)
		{
			runningStatus = 0;
			var msg = new SysExMessage(r);
			if (!msg.IsComplete)
			{
				sysexContinue = true;
			}
		}
		else if (cmd == 0xF7)
		{
			runningStatus = 0;
			if (sysexContinue)
			{
				var msg = new SysExContinuationMessage(r);
				if (msg.IsFinished)
				{
					sysexContinue = false;
				}
			}
			else
			{
				InsertMessage(ticks, new EscapeMessage(r));
			}
		}
		else if (cmd == 0xFF)
		{
			var msg = new MetaMessage(r);
			if (msg.Type == MetaMessageType.EndOfTrack)
			{
				foundEnd = true;
			}
			InsertMessage(ticks, msg);
		}
		else
		{
			throw new InvalidDataException($"Unknown MIDI command found at 0x{startOffset:X} (0x{cmd:X})");
		}
	}

	/// <summary>If there are other events at <paramref name="ticks"/>, <paramref name="msg"/> will be inserted after them.</summary>
	public void InsertMessage(int ticks, MIDIMessage msg)
	{
		if (ticks < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ticks), ticks, null);
		}

		var e = new MIDIEvent(ticks, msg);

		if (NumEvents == 0)
		{
			First = e;
			Last = e;
		}
		else if (ticks < First!.Ticks)
		{
			e.Next = First;
			First.Prev = e;
			First = e;
		}
		else if (ticks >= Last!.Ticks)
		{
			e.Prev = Last;
			Last.Next = e;
			Last = e;
		}
		else // Somewhere between
		{
			MIDIEvent next = First;

			while (next.Ticks <= ticks)
			{
				next = next.Next!;
			}

			MIDIEvent prev = next.Prev!;

			e.Next = next;
			e.Prev = prev;
			prev.Next = e;
			next.Prev = e;
		}

		NumEvents++;
	}

	public override void Write(EndianBinaryWriter w)
	{
		w.WriteChars_Count(EXPECTED_NAME, 4);

		long sizeOffset = w.Stream.Position;
		w.WriteUInt32(0); // We will update the size later

		byte runningStatus = 0;
		bool foundEnd = false;
		bool sysexContinue = false;
		for (MIDIEvent? e = First; e is not null; e = e.Next)
		{
			if (foundEnd)
			{
				throw new InvalidDataException($"Events found after the {nameof(MetaMessageType.EndOfTrack)} {nameof(MetaMessage)}");
			}

			WriteEvent(w, e, ref runningStatus, ref foundEnd, ref sysexContinue);
		}
		if (!foundEnd)
		{
			throw new InvalidDataException($"You must insert an {nameof(MetaMessageType.EndOfTrack)} {nameof(MetaMessage)}");
		}

		// Update size now
		long endOffset = w.Stream.Position;
		uint size = (uint)(endOffset - sizeOffset - 4);
		w.Stream.Position = sizeOffset;
		w.WriteUInt32(size);

		w.Stream.Position = endOffset; // Go back to the end
	}
	private static void WriteEvent(EndianBinaryWriter w, MIDIEvent e, ref byte runningStatus, ref bool foundEnd, ref bool sysexContinue)
	{
		Utils.WriteVariableLength(w, e.DeltaTicks);

		MIDIMessage msg = e.Message;
		byte cmd = msg.GetCMDByte();
		if (sysexContinue && cmd != 0xF7)
		{
			throw new InvalidDataException($"{nameof(SysExContinuationMessage)} was missing");
		}

		if (cmd is >= 0x80 and <= 0xEF)
		{
			if (runningStatus != cmd)
			{
				runningStatus = cmd;
				w.WriteByte(cmd);
			}
		}
		else if (cmd == 0xF0)
		{
			runningStatus = 0;
			var sysex = (SysExMessage)msg;
			if (!sysex.IsComplete)
			{
				sysexContinue = true;
			}
			w.WriteByte(0xF0);
		}
		else if (cmd == 0xF7)
		{
			runningStatus = 0;
			if (sysexContinue)
			{
				var sysex = (SysExContinuationMessage)msg;
				if (sysex.IsFinished)
				{
					sysexContinue = false;
				}
			}
			w.WriteByte(0xF0);
		}
		else if (cmd == 0xFF)
		{
			var meta = (MetaMessage)msg;
			if (meta.Type == MetaMessageType.EndOfTrack)
			{
				foundEnd = true;
			}
			w.WriteByte(0xFF);
		}
		else
		{
			throw new InvalidDataException($"Unknown MIDI command 0x{cmd:X}");
		}

		msg.Write(w);
	}
}
