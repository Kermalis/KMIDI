namespace Kermalis.MIDI;

public sealed class MIDIEvent
{
	public int Ticks { get; internal set; }
	/// <summary>How many ticks are between this event and the previous one. If this is the first event in the track, then it is equal to <see cref="Ticks"/></summary>
	public int DeltaTicks => Prev is null ? Ticks : Ticks - Prev.Ticks;

	public MIDIMessage Message { get; set; }

	public MIDIEvent? Prev { get; internal set; }
	public MIDIEvent? Next { get; internal set; }

	internal MIDIEvent(int ticks, MIDIMessage msg)
	{
		Ticks = ticks;
		Message = msg;
	}

	public override string ToString()
	{
		return string.Format("@{0} = {1}", Ticks, Message);
	}
}