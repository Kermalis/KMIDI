namespace Kermalis.MIDI;

public interface IMIDIChannelMessage
{
	byte Channel { get; }
}
public interface ISysExMessage
{
	byte[] Data { get; }
}