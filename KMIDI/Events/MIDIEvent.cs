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
public interface IMIDIEvent<T> : IMIDIEvent
	where T : MIDIMessage
{
	new T Msg { get; }
}

internal abstract class MIDIEvent : IMIDIEvent
{
	public int Ticks { get; }
	public MIDIEvent? IPrev { get; set; }
	public MIDIEvent? INext { get; set; }

	public abstract MIDIMessage Msg { get; }

	public IMIDIEvent? Prev => IPrev;
	public IMIDIEvent? Next => INext;

	protected MIDIEvent(int ticks)
	{
		Ticks = ticks;
	}

	public override string ToString()
	{
		return string.Format("@{0} = {1}", Ticks, Msg);
	}
}
internal sealed class MIDIEvent<T> : MIDIEvent, IMIDIEvent<T>
	where T : MIDIMessage
{
	public T IMsg { get; set; }

	public override MIDIMessage Msg => IMsg;
	T IMIDIEvent<T>.Msg => IMsg;

	public MIDIEvent(int ticks, T msg)
		: base(ticks)
	{
		IMsg = msg;
	}
}