using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Text;

namespace Kermalis.MIDI;

public sealed class MetaMessage : MIDIMessage
{
	public MetaMessageType Type { get; }
	public byte[] Data { get; }

	internal MetaMessage(EndianBinaryReader r)
	{
		long startPos = r.Stream.Position;

		Type = r.ReadEnum<MetaMessageType>();
		if (Type >= MetaMessageType.MAX)
		{
			Utils.ThrowInvalidMessageDataException(nameof(MetaMessage), nameof(Type), startPos, Type);
		}
		int expectedLen = GetExpectedLength(Type);

		int len = Utils.ReadVariableLength(r);
		if (expectedLen != -1 && expectedLen != len)
		{
			throw new InvalidDataException($"{nameof(MetaMessage)} at 0x{startPos:X} had an invalid length for {Type}: {len}. Expected {expectedLen}");
		}

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
		int expectedLen = GetExpectedLength(type);

		if (expectedLen == -1)
		{
			if (!Utils.IsValidVariableLengthValue(data.Length))
			{
				throw new ArgumentException($"{nameof(EscapeMessage)} data length must be [0, 0x0FFFFFFF]");
			}
		}
		else if (data.Length != expectedLen)
		{
			throw new ArgumentException($"{nameof(EscapeMessage)} data length must be {expectedLen} for {type}");
		}

		Type = type;
		Data = data;
	}

	public static MetaMessage CreateSequenceNumberMessage(ushort sequenceID)
	{
		byte[] data = new byte[2];
		EndianBinaryPrimitives.WriteUInt16_Unsafe(data, sequenceID, Endianness.BigEndian);
		return new MetaMessage(MetaMessageType.SequenceNumber, data);
	}
	public void ReadSequenceNumberMessage(out ushort sequenceID)
	{
		sequenceID = EndianBinaryPrimitives.ReadUInt16(Data, Endianness.BigEndian);
	}

	public static MetaMessage CreateTextMessage(MetaMessageType type, string text)
	{
		if (type is < MetaMessageType.Text or > MetaMessageType.Reserved_F)
		{
			throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}

		byte[] data = Encoding.ASCII.GetBytes(text);
		return new MetaMessage(type, data);
	}
	public void ReadTextMessage(out string text)
	{
		text = Encoding.ASCII.GetString(Data);
	}

	public static MetaMessage CreateMIDIChannelPrefixMessage(byte channel)
	{
		Utils.ValidateMIDIChannel(channel);

		byte[] data = new byte[1];
		data[0] = channel;
		return new MetaMessage(MetaMessageType.MIDIChannelPrefix, data);
	}
	public void ReadMIDIChannelPrefixMessage(out byte channel)
	{
		channel = Data[0];
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
		if (microsecondsPerQuarterNote > 0xFFFFFF)
		{
			throw new ArgumentOutOfRangeException(nameof(microsecondsPerQuarterNote), microsecondsPerQuarterNote, null);
		}

		byte[] data = new byte[3];
		data[2] = (byte)microsecondsPerQuarterNote;
		data[1] = (byte)(microsecondsPerQuarterNote >> 8);
		data[0] = (byte)(microsecondsPerQuarterNote >> 16);
		return new MetaMessage(MetaMessageType.Tempo, data);
	}
	public void ReadTempoMessage(out uint microsecondsPerQuarterNote, out decimal beatsPerMinute)
	{
		microsecondsPerQuarterNote = Data[2] | ((uint)Data[1] << 8) | ((uint)Data[0] << 16);
		beatsPerMinute = 60_000_000m / microsecondsPerQuarterNote;
	}

	public static MetaMessage CreateSMPTEOffsetMessage(byte hour, byte minute, byte second, byte frame, byte fractionalFrame)
	{
		// TODO: Verify

		byte[] data = new byte[5];
		data[0] = hour;
		data[1] = minute;
		data[2] = second;
		data[3] = frame;
		data[4] = fractionalFrame;
		return new MetaMessage(MetaMessageType.SMPTEOffset, data);
	}
	public void ReadSMPTEOffsetMessage(out byte hour, out byte minute, out byte second, out byte frame, out byte fractionalFrame)
	{
		hour = Data[0];
		minute = Data[1];
		second = Data[2];
		frame = Data[3];
		fractionalFrame = Data[4];
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
	public void ReadTimeSignatureMessage(out byte numerator, out byte denominator, out byte clocksPerMetronomeClick, out byte num32ndNotesPerQuarterNote)
	{
		numerator = Data[0];
		denominator = (byte)Math.Pow(Data[1], 2);
		clocksPerMetronomeClick = Data[2];
		num32ndNotesPerQuarterNote = Data[3];
	}

	public static MetaMessage CreateKeySignatureMessage(KeySignatureSF sf, KeySignatureMI mi)
	{
		if (sf is <= KeySignatureSF.MIN or >= KeySignatureSF.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(sf), sf, null);
		}
		if (mi >= KeySignatureMI.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(mi), mi, null);
		}

		byte[] data = new byte[2];
		data[0] = (byte)sf;
		data[1] = (byte)mi;
		return new MetaMessage(MetaMessageType.KeySignature, data);
	}
	public void ReadKeySignatureMessage(out KeySignatureSF sf, out KeySignatureMI mi)
	{
		sf = (KeySignatureSF)Data[0];
		mi = (KeySignatureMI)Data[1];
	}

	public static MetaMessage CreateProprietaryEventMessage(ushort manufacturer, ReadOnlySpan<byte> msgData)
	{
		Span<byte> id = stackalloc byte[3];
		int idLen;
		if (manufacturer is 0 or > byte.MaxValue)
		{
			idLen = 3;
			id[0] = 0;
			EndianBinaryPrimitives.WriteUInt16(id.Slice(1), manufacturer, Endianness.BigEndian);
		}
		else
		{
			idLen = 1;
			id[0] = (byte)manufacturer;
		}

		byte[] data = new byte[idLen + msgData.Length];
		id.Slice(0, idLen).CopyTo(data);
		msgData.CopyTo(data.AsSpan(idLen));
		return new MetaMessage(MetaMessageType.ProprietaryEvent, data);
	}
	public void ReadProprietaryEventMessage(out ushort manufacturer, out ReadOnlySpan<byte> msgData)
	{
		manufacturer = Data[0];
		int idLen = 1;
		if (manufacturer == 0)
		{
			manufacturer = EndianBinaryPrimitives.ReadUInt16(Data.AsSpan(1, 2), Endianness.BigEndian);
			idLen = 3;
		}
		msgData = Data.AsSpan(idLen);
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

	private static int GetExpectedLength(MetaMessageType type)
	{
		switch (type)
		{
			case MetaMessageType.SequenceNumber: return 2;
			case MetaMessageType.MIDIChannelPrefix: return 1;
			case MetaMessageType.EndOfTrack: return 0;
			case MetaMessageType.Tempo: return 3;
			case MetaMessageType.SMPTEOffset: return 5;
			case MetaMessageType.TimeSignature: return 4;
			case MetaMessageType.KeySignature: return 2;
		}
		return -1; // Section 3 - Not required to support all types
	}

	public override string ToString()
	{
		string? arg;

		switch (Type)
		{
			case MetaMessageType.SequenceNumber:
			{
				ReadSequenceNumberMessage(out ushort sequenceID);
				arg = sequenceID.ToString();
				break;
			}
			case MetaMessageType.Text:
			case MetaMessageType.Copyright:
			case MetaMessageType.TrackName:
			case MetaMessageType.InstrumentName:
			case MetaMessageType.Lyric:
			case MetaMessageType.Marker:
			case MetaMessageType.CuePoint:
			case MetaMessageType.ProgramName:
			case MetaMessageType.DeviceName:
			case MetaMessageType.Reserved_A:
			case MetaMessageType.Reserved_B:
			case MetaMessageType.Reserved_C:
			case MetaMessageType.Reserved_D:
			case MetaMessageType.Reserved_E:
			case MetaMessageType.Reserved_F:
			{
				ReadTextMessage(out string text);
				arg = '\"' + text + '\"';
				break;
			}
			case MetaMessageType.MIDIChannelPrefix:
			{
				ReadMIDIChannelPrefixMessage(out byte channel);
				arg = channel.ToString();
				break;
			}
			case MetaMessageType.EndOfTrack:
			{
				arg = null;
				break;
			}
			case MetaMessageType.Tempo:
			{
				ReadTempoMessage(out uint microsecondsPerQuarterNote, out decimal beatsPerMinute);
				arg = string.Format("MicrosecondsPerQuarterNote: {0} ({1} bpm)", microsecondsPerQuarterNote, beatsPerMinute);
				break;
			}
			case MetaMessageType.SMPTEOffset:
			{
				ReadSMPTEOffsetMessage(out byte hour, out byte minute, out byte second, out byte frame, out byte fractionalFrame);
				arg = string.Format("Hour: {0}, Minute: {1}, Second: {2}, Frame: {3}, FractionalFrame: {4}", hour, minute, second, frame, fractionalFrame);
				break;
			}
			case MetaMessageType.TimeSignature:
			{
				ReadTimeSignatureMessage(out byte numerator, out byte denominator, out byte clocksPerMetronomeClick, out byte num32ndNotesPerQuarterNote);
				arg = string.Format("{0}/{1}, ClocksPerMetronomeClick: {2}, Num32ndNotesPerQuarterNote: {3}", numerator, denominator, clocksPerMetronomeClick, num32ndNotesPerQuarterNote);
				break;
			}
			case MetaMessageType.KeySignature:
			{
				ReadKeySignatureMessage(out KeySignatureSF sf, out KeySignatureMI mi);
				arg = string.Format("{0} {1}", sf, mi);
				break;
			}
			case MetaMessageType.ProprietaryEvent:
			{
				ReadProprietaryEventMessage(out ushort manufacturer, out ReadOnlySpan<byte> msgData);
				arg = string.Format("Manufacturer: {0}, Length: {1}", manufacturer, msgData.Length);
				break;
			}
			default:
			{
				arg = string.Format("Length: {0}", Data.Length);
				break;
			}
		}

		if (arg is null)
		{
			return string.Format("{0} [{1}]", nameof(MetaMessage), Type);
		}
		return string.Format("{0} [{1}: {2}]", nameof(MetaMessage), Type, arg);
	}
}