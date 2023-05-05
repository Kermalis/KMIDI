namespace Kermalis.MIDI;

public interface IMIDIEvent
{
	int Ticks { get; }
	/// <summary>How many ticks are between this event and the previous one. If this is the first event in the track, then it is equal to <see cref="Ticks"/></summary>
	int DeltaTicks => Prev is null ? Ticks : Ticks - Prev.Ticks;

	MIDIMessage Msg { get; }

	IMIDIEvent? Prev { get; }
	IMIDIEvent? Next { get; }
}
// This is required since we can't cast to a non-generic MIDIEvent
internal interface IMIDIEvent_Internal : IMIDIEvent
{
	int ITicks { get; set; }
	IMIDIEvent_Internal? IPrev { get; set; }
	IMIDIEvent_Internal? INext { get; set; }
}

public sealed class MIDIEvent<T> : IMIDIEvent_Internal
	where T : MIDIMessage
{
	internal IMIDIEvent_Internal IThis => this;

	int IMIDIEvent_Internal.ITicks { get; set; }
	IMIDIEvent_Internal? IMIDIEvent_Internal.IPrev { get; set; }
	IMIDIEvent_Internal? IMIDIEvent_Internal.INext { get; set; }

	public int Ticks => IThis.ITicks;

	MIDIMessage IMIDIEvent.Msg => Msg;
	public T Msg { get; set; }

	public IMIDIEvent? Prev => IThis.IPrev;
	public IMIDIEvent? Next => IThis.INext;

	internal MIDIEvent(int ticks, T msg)
	{
		IThis.ITicks = ticks;
		Msg = msg;
	}

	public override string ToString()
	{
		return string.Format("@{0} = {1}", Ticks, Msg);
	}
}