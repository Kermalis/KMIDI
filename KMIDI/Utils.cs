using Kermalis.EndianBinaryIO;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Kermalis.MIDI;

internal static class Utils
{
	public static bool IsPowerOfTwo(int value)
	{
		return (value & (value - 1)) == 0;
	}

	public static int ReadVariableLength(EndianBinaryReader r)
	{
		// 28 bits allowed
		const int LIMIT = 4; // (varlen)0x7F_FF_FF_FF represents (uint)0x0F_FF_FF_FF

		uint value = 0;
		byte curByte;

		for (int shift = 0; shift < LIMIT * 7; shift += 7)
		{
			curByte = r.ReadByte();
			value |= (curByte & 0x7Fu) << shift;

			if (curByte <= 0x7Fu)
			{
				return (int)value;
			}
		}
		throw new InvalidDataException("Variable length value was more than 28 bits");
	}
	public static void WriteVariableLength(EndianBinaryWriter w, int value)
	{
		ValidateVariableLengthValue(value);

		// value = 0x0F_FF_FF_FF
		// WriteByte 0xFF
		// value = 0x1F_FF_FF
		// WriteByte 0xFF
		// value = 0x3F_FF
		// WriteByte 0xFF
		// value = 0x7F
		// WriteByte 0x7F

		while (value > 0x7F)
		{
			w.WriteByte((byte)(value | ~0x7Fu));
			value >>= 7;
		}
		w.WriteByte((byte)value);
	}

	public static void ValidateMIDIChannel(byte channel)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
	}
	public static bool IsValidVariableLengthValue(int value)
	{
		return value is >= 0 and <= 0x0FFFFFFF; // Section 1.1
	}
	private static void ValidateVariableLengthValue(int value)
	{
		if (!IsValidVariableLengthValue(value))
		{
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		}
	}
	[DoesNotReturn]
	public static void ThrowInvalidMessageDataException(string msgType, string msgParam, long pos, object value)
	{
		throw new InvalidDataException(string.Format("Invalid {0} {1} at 0x{2:X} ({3})", msgType, msgParam, pos, value));
	}
}
