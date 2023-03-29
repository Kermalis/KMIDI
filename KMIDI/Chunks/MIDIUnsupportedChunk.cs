using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class MIDIUnsupportedChunk : MIDIChunk
{
	/// <summary>Length 4</summary>
	public string ChunkName { get; }
	public byte[] Data { get; }

	public MIDIUnsupportedChunk(string chunkName, byte[] data)
	{
		if (chunkName.Length != 4)
		{
			throw new ArgumentOutOfRangeException(nameof(chunkName), chunkName, null);
		}

		ChunkName = chunkName;
		Data = data;
	}
	internal MIDIUnsupportedChunk(string chunkName, uint size, EndianBinaryReader r)
	{
		ChunkName = chunkName;
		Data = new byte[size];
		r.ReadBytes(Data);
	}

	public override void Write(EndianBinaryWriter w)
	{
		w.WriteChars_Count(ChunkName, 4);
		w.WriteUInt32((uint)Data.Length);

		w.WriteBytes(Data);
	}

	public override string ToString()
	{
		return $"<{ChunkName}> [{Data.Length} bytes]";
	}
}
