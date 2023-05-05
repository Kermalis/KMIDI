using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Text;

namespace Kermalis.MIDI;

public sealed class MIDITrackChunk : MIDIChunk
{
	internal const string EXPECTED_NAME = "MTrk";

	private IMIDIEvent_Internal? _first;
	private IMIDIEvent_Internal? _last;

	public IMIDIEvent? First => _first;
	public IMIDIEvent? Last => _last;

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
	public MIDIEvent<T> InsertMessage<T>(int ticks, T msg)
		where T : MIDIMessage
	{
		if (ticks < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ticks), ticks, null);
		}

		var ret = new MIDIEvent<T>(ticks, msg);
		IMIDIEvent_Internal e = ret.IThis;

		if (NumEvents == 0)
		{
			_first = e;
			_last = e;
		}
		else if (ticks < _first!.Ticks)
		{
			e.INext = _first;
			_first.IPrev = e;
			_first = e;
		}
		else if (ticks >= _last!.Ticks)
		{
			e.IPrev = _last;
			_last.INext = e;
			_last = e;
		}
		else // Somewhere between
		{
			IMIDIEvent_Internal next = _first;

			while (next.Ticks <= ticks)
			{
				next = next.INext!;
			}

			IMIDIEvent_Internal prev = next.IPrev!;

			e.INext = next;
			e.IPrev = prev;
			prev.INext = e;
			next.IPrev = e;
		}

		NumEvents++;
		return ret;
	}
	public bool RemoveEvent(IMIDIEvent ev)
	{
		if (ev is not IMIDIEvent_Internal e)
		{
			return false;
		}
		if (NumEvents == 0)
		{
			return false;
		}

		IMIDIEvent_Internal first = _first!;
		IMIDIEvent_Internal last = _last!;
		if (NumEvents == 1)
		{
			if (e == first && e == last)
			{
				_first = null;
				_last = null;
				NumEvents = 0;
				return true;
			}
			// If it wasn't the only event, then it's not in this track
			return false;
		}

		// Below here, we have at least 2 events
		if (e == first)
		{
			_first = e.INext!;
			_first.IPrev = null;
			NumEvents--;
			return true;
		}
		if (e == last)
		{
			_last = e.IPrev!;
			_last.INext = null;
			NumEvents--;
			return true;
		}

		// Either e is not in this track, or it's in the range (first, last)
		for (IMIDIEvent_Internal i = first.INext!; i != last; i = i.INext!)
		{
			if (e == i)
			{
				IMIDIEvent_Internal prev = e.IPrev!;
				IMIDIEvent_Internal next = e.INext!;
				prev.INext = next;
				next.IPrev = prev;
				NumEvents--;
				return true;
			}
		}

		return false;
	}

	public override void Write(EndianBinaryWriter w)
	{
		w.WriteChars_Count(EXPECTED_NAME, 4);

		long sizeOffset = w.Stream.Position;
		w.WriteUInt32(0); // We will update the size later

		byte runningStatus = 0;
		bool foundEnd = false;
		bool sysexContinue = false;
		for (IMIDIEvent? e = _first; e is not null; e = e.Next)
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
	private static void WriteEvent(EndianBinaryWriter w, IMIDIEvent e, ref byte runningStatus, ref bool foundEnd, ref bool sysexContinue)
	{
		Utils.WriteVariableLength(w, e.DeltaTicks);

		MIDIMessage msg = e.Msg;
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

	public override string ToString()
	{
		var str = new StringBuilder($"<{EXPECTED_NAME}>");
		str.AppendLine();

		str.AppendLine($"\t{nameof(NumEvents)}: {NumEvents}");
		str.AppendLine($"\t{nameof(NumTicks)}: {NumTicks}");

		for (IMIDIEvent? e = _first; e is not null; e = e.Next)
		{
			str.Append("\t\t");
			str.AppendLine(e.ToString());
		}

		return str.ToString();
	}
}
