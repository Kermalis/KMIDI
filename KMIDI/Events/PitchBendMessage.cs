using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class PitchBendMessage : MIDIMessage, IMIDIChannelMessage
{
	private const int CENTER = 0x2000;

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
	public PitchBendMessage(byte channel, int pitch)
	{
		Utils.ValidateMIDIChannel(channel);
		GetLSBAndMSBFromPitchInt(pitch, out byte lsb, out byte msb);

		Channel = channel;
		LSB = lsb;
		MSB = msb;
	}

	/// <summary>Returns a value in the range [-8_192, 8_191]</summary>
	public int GetPitchAsInt()
	{
		uint uPitch = ((uint)MSB << 7) | LSB;
		return (int)uPitch - CENTER;
	}
	/// <summary><inheritdoc cref="GetPitchAsInt()"/></summary>
	public static int GetPitchAsInt(byte lsb, byte msb)
	{
		if (lsb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(lsb), lsb, null);
		}
		if (msb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(msb), msb, null);
		}

		uint uPitch = ((uint)msb << 7) | lsb;
		return (int)uPitch - CENTER;
	}
	public static void GetLSBAndMSBFromPitchInt(int pitch, out byte lsb, out byte msb)
	{
		if (pitch is > (0x3FFF - CENTER) or < (0 - CENTER))
		{
			throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Pitch must be [-8_192, 8_191]");
		}

		pitch += CENTER; // Now [0, 16_383]

		msb = (byte)(pitch >> 7);
		lsb = (byte)(pitch & 0x7F);
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
