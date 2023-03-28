using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.MIDI;

public sealed class SysExContinuationMessage : MIDIMessage
{
	public byte[] Data { get; }

	public bool IsFinished => Data[Data.Length - 1] == 0xF7;

	internal SysExContinuationMessage(EndianBinaryReader r)
	{
		long offset = r.Stream.Position;

		int len = Utils.ReadVariableLength(r);
		if (len == 0)
		{
			throw new InvalidDataException($"{nameof(SysExContinuationMessage)} at 0x{offset:X} was empty");
		}

		Data = new byte[len];
		r.ReadBytes(Data);
	}

	public SysExContinuationMessage(byte[] data)
	{
		if (data.Length == 0 || !Utils.IsValidVariableLengthValue(data.Length))
		{
			throw new ArgumentException($"{nameof(SysExContinuationMessage)} data length must be [1, 0x0FFFFFFF]");
		}

		Data = data;
	}

	internal override byte GetCMDByte()
	{
		return 0xF7;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		Utils.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}
}
