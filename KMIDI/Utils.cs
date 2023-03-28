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
		// (varlen)0x7F_FF_FF_FF represents (uint)0x0F_FF_FF_FF

		int value = r.ReadByte();
		int numBytesRead = 1;

		if ((value & 0x80) != 0)
		{
			value &= 0x7F;

			while (true)
			{
				if (numBytesRead >= 4)
				{
					throw new InvalidDataException("Variable length value was more than 28 bits");
				}

				byte curByte = r.ReadByte();
				numBytesRead++;

				value = (value << 7) + (curByte & 0x7F);
				if ((curByte & 0x80) == 0)
				{
					break;
				}
			}
		}

		return value;
	}
	public static void WriteVariableLength(EndianBinaryWriter w, int value)
	{
		ValidateVariableLengthValue(value);

		int buffer = value & 0x7F;
		while ((value >>= 7) > 0)
		{
			buffer <<= 8;
			buffer |= 0x80;
			buffer += value & 0x7F;
		}
		while (true)
		{
			w.WriteByte((byte)buffer);
			if ((buffer & 0x80) == 0)
			{
				break;
			}
			buffer >>= 8;
		}
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
