using Kermalis.EndianBinaryIO;
using System;
using System.Text;

namespace Kermalis.MIDI;

public sealed class MetaMessage : MIDIMessage
{
	public MetaMessageType Type { get; }
	public byte[] Data { get; }

	internal MetaMessage(EndianBinaryReader r)
	{
		Type = r.ReadEnum<MetaMessageType>();
		if (Type >= MetaMessageType.MAX)
		{
			Utils.ThrowInvalidMessageDataException(nameof(MetaMessage), nameof(Type), r.Stream.Position - 1, Type);
		}

		int len = Utils.ReadVariableLength(r);
		if (len == 0)
		{
			Data = Array.Empty<byte>();
		}
		else
		{
			Data = new byte[len];
			r.ReadBytes(Data);
		}
	}

	public MetaMessage(MetaMessageType type, byte[] data)
	{
		if (type >= MetaMessageType.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}

		Type = type;
		Data = data;
	}

	public static MetaMessage CreateTempoMessage(in decimal beatsPerMinute)
	{
		if (beatsPerMinute <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(beatsPerMinute), beatsPerMinute, null);
		}

		return CreateTempoMessage((uint)(60_000_000m / beatsPerMinute));
	}
	public static MetaMessage CreateTempoMessage(uint microsecondsPerQuarterNote)
	{
		byte[] data = new byte[3];
		data[2] = (byte)microsecondsPerQuarterNote;
		data[1] = (byte)(microsecondsPerQuarterNote >> 8);
		data[0] = (byte)(microsecondsPerQuarterNote >> 16);
		return new MetaMessage(MetaMessageType.Tempo, data);
	}
	public bool TryReadTempoMessage(out uint microsecondsPerQuarterNote, out decimal beatsPerMinute)
	{
		if (Type != MetaMessageType.Tempo || Data.Length != 3)
		{
			microsecondsPerQuarterNote = 0;
			beatsPerMinute = 0;
			return false;
		}

		microsecondsPerQuarterNote = Data[2] | ((uint)Data[1] << 8) | ((uint)Data[0] << 16);
		beatsPerMinute = 60_000_000m / microsecondsPerQuarterNote;
		return true;
	}

	public static MetaMessage CreateTimeSignatureMessage(byte numerator, byte denominator, byte clocksPerMetronomeClick = 24, byte num32ndNotesPerQuarterNote = 8)
	{
		if (numerator == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(numerator), numerator, null);
		}
		if (denominator is < 2 or > 32)
		{
			throw new ArgumentOutOfRangeException(nameof(denominator), denominator, null);
		}
		if (!Utils.IsPowerOfTwo(denominator))
		{
			throw new ArgumentException("Denominator must be a power of 2", nameof(denominator));
		}
		if (clocksPerMetronomeClick == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(clocksPerMetronomeClick), clocksPerMetronomeClick, null);
		}
		if (num32ndNotesPerQuarterNote == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(num32ndNotesPerQuarterNote), num32ndNotesPerQuarterNote, null);
		}

		byte[] data = new byte[4];
		data[0] = numerator;
		data[1] = (byte)Math.Log(denominator, 2);
		data[2] = clocksPerMetronomeClick;
		data[3] = num32ndNotesPerQuarterNote;
		return new MetaMessage(MetaMessageType.TimeSignature, data);
	}
	public bool TryReadTimeSignatureMessage(out byte numerator, out byte denominator, out byte clocksPerMetronomeClick, out byte num32ndNotesPerQuarterNote)
	{
		if (Type != MetaMessageType.TimeSignature || Data.Length != 4)
		{
			numerator = 0;
			denominator = 0;
			clocksPerMetronomeClick = 0;
			num32ndNotesPerQuarterNote = 0;
			return false;
		}

		numerator = Data[0];
		denominator = (byte)Math.Pow(Data[1], 2);
		clocksPerMetronomeClick = Data[2];
		num32ndNotesPerQuarterNote = Data[3];
		return true;
	}

	internal override byte GetCMDByte()
	{
		return 0xFF;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Type);
		Utils.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}

	public override string ToString()
	{
		return string.Format("{0} [{1}: \"{2}\"]", nameof(MetaMessage), Type, Encoding.ASCII.GetString(Data));
	}
}