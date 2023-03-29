using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class PitchBendMessage : MIDIMessage
{
	public byte Channel { get; }

	public byte LSB { get; }
	public byte MSB { get; }

	internal PitchBendMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		LSB = r.ReadByte();
		if (LSB > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(PitchBendMessage), nameof(LSB), r.Stream.Position - 1, LSB);
		}

		MSB = r.ReadByte();
		if (MSB > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(PitchBendMessage), nameof(MSB), r.Stream.Position - 1, MSB);
		}
	}

	public PitchBendMessage(byte channel, byte lsb, byte msb)
	{
		Utils.ValidateMIDIChannel(channel);
		if (lsb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(lsb), lsb, null);
		}
		if (msb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(msb), msb, null);
		}

		Channel = channel;
		LSB = lsb;
		MSB = msb;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xE0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteByte(LSB);
		w.WriteByte(MSB);
	}

	public override string ToString()
	{
		return $"{nameof(PitchBendMessage)} [{nameof(Channel)} {Channel}"
			+ $", {nameof(LSB)}: {LSB}"
			+ $", {nameof(MSB)}: {MSB}"
			+ ']';
	}
}
