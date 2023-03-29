using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class ProgramChangeMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDIProgram Program { get; }

	internal ProgramChangeMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Program = r.ReadEnum<MIDIProgram>();
		if (Program >= MIDIProgram.MAX)
		{
			Utils.ThrowInvalidMessageDataException(nameof(ProgramChangeMessage), nameof(Program), r.Stream.Position - 1, Program);
		}
	}

	public ProgramChangeMessage(byte channel, MIDIProgram program)
	{
		Utils.ValidateMIDIChannel(channel);
		if (program >= MIDIProgram.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(program), program, null);
		}

		Channel = channel;
		Program = program;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xC0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Program);
	}

	public override string ToString()
	{
		return $"{nameof(ProgramChangeMessage)} [{nameof(Channel)} {Channel}"
			+ $", {nameof(Program)}: {Program}"
			+ ']';
	}
}