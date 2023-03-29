using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class NoteOnMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDINote Note { get; }
	public byte Velocity { get; }

	internal NoteOnMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Note = r.ReadEnum<MIDINote>();
		if (Note >= MIDINote.MAX)
		{
			Utils.ThrowInvalidMessageDataException(nameof(NoteOnMessage), nameof(Note), r.Stream.Position - 1, Note);
		}

		Velocity = r.ReadByte();
		if (Velocity > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(NoteOnMessage), nameof(Velocity), r.Stream.Position - 1, Velocity);
		}
	}

	public NoteOnMessage(byte channel, MIDINote note, byte velocity)
	{
		Utils.ValidateMIDIChannel(channel);
		if (note >= MIDINote.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(note), note, null);
		}
		if (velocity > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(velocity), velocity, null);
		}

		Channel = channel;
		Note = note;
		Velocity = velocity;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0x90 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Note);
		w.WriteByte(Velocity);
	}

	public override string ToString()
	{
		return $"{nameof(NoteOnMessage)} [{nameof(Channel)} {Channel}"
			+ $", {nameof(Note)}: {Note}"
			+ $", {nameof(Velocity)}: {Velocity}"
			+ ']';
	}
}
