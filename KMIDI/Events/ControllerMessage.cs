using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MIDI;

public sealed class ControllerMessage : MIDIMessage
{
	public byte Channel { get; }

	public ControllerType Controller { get; }
	public byte Value { get; }

	internal ControllerMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Controller = r.ReadEnum<ControllerType>();
		if (Controller >= ControllerType.MAX)
		{
			Utils.ThrowInvalidMessageDataException(nameof(ControllerMessage), nameof(Controller), r.Stream.Position - 1, Controller);
		}

		Value = r.ReadByte();
		if (Value > 127)
		{
			Utils.ThrowInvalidMessageDataException(nameof(ControllerMessage), nameof(Value), r.Stream.Position - 1, Value);
		}
	}

	public ControllerMessage(byte channel, ControllerType controller, byte value)
	{
		Utils.ValidateMIDIChannel(channel);
		if (controller >= ControllerType.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(controller), controller, null);
		}
		if (value > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		}

		Channel = channel;
		Controller = controller;
		Value = value;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xB0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Controller);
		w.WriteByte(Value);
	}

	public override string ToString()
	{
		return $"{nameof(ControllerMessage)} [{nameof(Channel)} {Channel}"
			+ $", {nameof(Controller)}: {Controller}"
			+ $", {nameof(Value)}: {Value}"
			+ ']';
	}
}