using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.MIDI;

public sealed class SysExMessage : MIDIMessage, ISysExMessage
{
	public byte[] Data { get; }

	public bool IsComplete => Data[Data.Length - 1] == 0xF7;

	internal SysExMessage(EndianBinaryReader r)
	{
		long offset = r.Stream.Position;

		int len = Utils.ReadVariableLength(r);
		if (len == 0)
		{
			throw new InvalidDataException($"{nameof(SysExMessage)} at 0x{offset:X} was empty");
		}

		Data = new byte[len];
		r.ReadBytes(Data);
	}

	public SysExMessage(byte[] data)
	{
		if (data.Length == 0 || !Utils.IsValidVariableLengthValue(data.Length))
		{
			throw new ArgumentException($"{nameof(SysExMessage)} data length must be [1, 0x0FFFFFFF]");
		}

		Data = data;
	}

	internal override byte GetCMDByte()
	{
		return 0xF0;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		Utils.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}

	public override string ToString()
	{
		return $"{nameof(SysExMessage)} [Length: {Data.Length}"
			+ $", {nameof(IsComplete)}: {IsComplete}"
			+ ']';
	}
}
