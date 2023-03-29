using Kermalis.EndianBinaryIO;
using System;
using System.Diagnostics;
using System.IO;

namespace Kermalis.MIDI;

// Section 2.1
public sealed class MIDIHeaderChunk : MIDIChunk
{
	internal const string EXPECTED_NAME = "MThd";

	public MIDIFormat Format { get; }
	public ushort NumTracks { get; internal set; }
	public TimeDivisionValue TimeDivision { get; }

	internal MIDIHeaderChunk(MIDIFormat format, TimeDivisionValue timeDivision)
	{
		if (format > MIDIFormat.Format2)
		{
			throw new ArgumentOutOfRangeException(nameof(format), format, null);
		}
		if (!timeDivision.IsValid())
		{
			throw new ArgumentOutOfRangeException(nameof(timeDivision), timeDivision, null);
		}

		Format = format;
		TimeDivision = timeDivision;
	}
	internal MIDIHeaderChunk(uint size, EndianBinaryReader r)
	{
		if (size < 6)
		{
			throw new InvalidDataException($"Invalid MIDI header length ({size})");
		}

		long endOffset = GetEndOffset(r, size);

		Format = r.ReadEnum<MIDIFormat>();
		if (Format > MIDIFormat.Format2)
		{
			// Section 2.2 states that unknown formats should be supported
			Debug.WriteLine($"Unknown MIDI format ({Format}), so behavior is unknown");
		}

		NumTracks = r.ReadUInt16();
		if (NumTracks == 0)
		{
			throw new InvalidDataException("MIDI has no tracks");
		}
		if (Format == MIDIFormat.Format0 && NumTracks != 1)
		{
			throw new InvalidDataException($"MIDI format 0 must have 1 track, but this MIDI has {NumTracks}");
		}

		TimeDivision = new TimeDivisionValue(r.ReadUInt16());
		if (!TimeDivision.IsValid())
		{
			throw new InvalidDataException($"Invalid MIDI time division ({TimeDivision})");
		}

		if (size > 6)
		{
			// Section 2.2 states that the length should be honored
			Debug.WriteLine($"MIDI Header was longer than 6 bytes ({size}), so the extra data is being ignored");
			EatRemainingBytes(r, endOffset, EXPECTED_NAME, size);
		}
	}

	public override void Write(EndianBinaryWriter w)
	{
		if (Format == MIDIFormat.Format0 && NumTracks != 1)
		{
			throw new InvalidDataException($"MIDI format 0 must have 1 track, but this MIDI has {NumTracks}");
		}

		w.WriteChars_Count(EXPECTED_NAME, 4);
		w.WriteUInt32(6);

		w.WriteEnum(Format);
		w.WriteUInt16(NumTracks);
		w.WriteUInt16(TimeDivision.RawValue);
	}

	public override string ToString()
	{
		return $"<{EXPECTED_NAME}>"
			+ $"{Environment.NewLine}\t{nameof(Format)}: {Format}"
			+ $"{Environment.NewLine}\t{nameof(NumTracks)}: {NumTracks}"
			+ $"{Environment.NewLine}\t{nameof(TimeDivision)}: {TimeDivision}";
	}
}
