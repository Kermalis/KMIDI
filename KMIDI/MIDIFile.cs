using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MIDI;

// https://www.music.mcgill.ca/~ich/classes/mumt306/StandardMIDIfileformat.html
// http://www.somascape.org/midi/tech/mfile.html
// TODO: Is a second headerchunk valid?
public sealed class MIDIFile
{
	public MIDIHeaderChunk HeaderChunk { get; }
	private readonly List<MIDIChunk> _nonHeaderChunks;

	public MIDIFile(MIDIFormat format, TimeDivisionValue timeDivision, int tracksInitialCapacity)
	{
		if (format == MIDIFormat.Format0 && tracksInitialCapacity != 1)
		{
			throw new ArgumentException("Format 0 must have 1 track", nameof(tracksInitialCapacity));
		}

		HeaderChunk = new MIDIHeaderChunk(format, timeDivision); // timeDivision validated here
		_nonHeaderChunks = new List<MIDIChunk>(tracksInitialCapacity);
	}
	public MIDIFile(Stream stream)
	{
		var r = new EndianBinaryReader(stream, endianness: Endianness.BigEndian, ascii: true);
		string chunkName = r.ReadString_Count(4);
		if (chunkName != MIDIHeaderChunk.EXPECTED_NAME)
		{
			throw new InvalidDataException("MIDI header was not at the start of the file");
		}

		HeaderChunk = (MIDIHeaderChunk)ReadChunk(r, alreadyReadName: chunkName);
		_nonHeaderChunks = new List<MIDIChunk>(HeaderChunk.NumTracks);

		while (stream.Position < stream.Length)
		{
			MIDIChunk c = ReadChunk(r);
			_nonHeaderChunks.Add(c);
		}

		int trackCount = CountTrackChunks();
		if (trackCount != HeaderChunk.NumTracks)
		{
			throw new InvalidDataException($"Unexpected track count: (Expected {HeaderChunk.NumTracks} but found {trackCount}");
		}
	}

	private static MIDIChunk ReadChunk(EndianBinaryReader r, string? alreadyReadName = null)
	{
		string chunkName = alreadyReadName ?? r.ReadString_Count(4);
		uint chunkSize = r.ReadUInt32();
		switch (chunkName)
		{
			case MIDIHeaderChunk.EXPECTED_NAME: return new MIDIHeaderChunk(chunkSize, r);
			case MIDITrackChunk.EXPECTED_NAME: return new MIDITrackChunk(chunkSize, r);
			default: return new MIDIUnsupportedChunk(chunkName, chunkSize, r);
		}
	}

	public void AddChunk(MIDIChunk c)
	{
		_nonHeaderChunks.Add(c);
		if (c is MIDITrackChunk)
		{
			HeaderChunk.NumTracks++;
		}
	}
	public bool RemoveChunk(MIDIChunk c)
	{
		bool success = _nonHeaderChunks.Remove(c);
		if (success && c is MIDITrackChunk)
		{
			HeaderChunk.NumTracks--;
		}
		return success;
	}
	public IEnumerable<MIDIChunk> EnumerateChunks(bool includeHeaderChunk)
	{
		if (includeHeaderChunk)
		{
			yield return HeaderChunk;
		}
		foreach (MIDIChunk c in _nonHeaderChunks)
		{
			yield return c;
		}
	}
	public IEnumerable<MIDITrackChunk> EnumerateTrackChunks()
	{
		foreach (MIDIChunk c in _nonHeaderChunks)
		{
			if (c is MIDITrackChunk tc)
			{
				yield return tc;
			}
		}
	}
	public int CountTrackChunks()
	{
		int count = 0;
		foreach (MIDIChunk c in _nonHeaderChunks)
		{
			if (c is MIDITrackChunk tc)
			{
				count++;
			}
		}
		return count;
	}
	public void SetNonHeaderChunks(IEnumerable<MIDIChunk> nonHeaderChunks)
	{
		_nonHeaderChunks.Clear();
		_nonHeaderChunks.AddRange(nonHeaderChunks);
	}

	public void Save(Stream stream)
	{
		var w = new EndianBinaryWriter(stream, endianness: Endianness.BigEndian, ascii: true);

		HeaderChunk.Write(w);

		foreach (MIDIChunk c in _nonHeaderChunks)
		{
			c.Write(w);
		}
	}
}
