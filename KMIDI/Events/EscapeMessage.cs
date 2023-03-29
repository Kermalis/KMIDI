using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class EscapeMessage : MIDIMessage
{
	public byte[] Data { get; }

	internal EscapeMessage(EndianBinaryReader r)
	{
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

	public EscapeMessage(byte[] data)
	{
		if (!Utils.IsValidVariableLengthValue(data.Length))
		{
			throw new ArgumentException($"{nameof(EscapeMessage)} data length must be [0, 0x0FFFFFFF]");
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

	public override string ToString()
	{
		return $"{nameof(EscapeMessage)} [Length: {Data.Length}]";
	}
}
