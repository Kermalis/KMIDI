﻿using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class PolyphonicPressureMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDINote Note { get; }
	public byte Pressure { get; }

	internal PolyphonicPressureMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Note = r.ReadEnum<MIDINote>();

		Pressure = r.ReadByte();
		if (Pressure > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(PolyphonicPressureMessage), nameof(Pressure), r.Stream.Position - 1, Pressure);
		}
	}

	public PolyphonicPressureMessage(byte channel, MIDINote note, byte pressure)
	{
		Utils.ValidateMIDIChannel(channel);
		if (pressure > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(pressure), pressure, null);
		}

		Channel = channel;
		Note = note;
		Pressure = pressure;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xA0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Note);
		w.WriteByte(Pressure);
	}
}
