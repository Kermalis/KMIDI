using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class ChannelPressureMessage : MIDIMessage
{
	public byte Channel { get; }

	public byte Pressure { get; }

	internal ChannelPressureMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Pressure = r.ReadByte();
		if (Pressure > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(ChannelPressureMessage), nameof(Pressure), r.Stream.Position - 1, Pressure);
		}
	}

	public ChannelPressureMessage(byte channel, byte pressure)
	{
		Utils.ValidateMIDIChannel(channel);
		if (pressure > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(pressure), pressure, null);
		}

		Channel = channel;
		Pressure = pressure;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xD0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteByte(Pressure);
	}

	public override string ToString()
	{
		return $"{nameof(ChannelPressureMessage)} [{nameof(Channel)} {Channel}"
			+ $", {nameof(Pressure)}: {Pressure}"
			+ ']';
	}
}
