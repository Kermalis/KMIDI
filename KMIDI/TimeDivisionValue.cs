namespace Kermalis.MIDI;

// Section 2.1
public readonly struct TimeDivisionValue
{
	public const int PPQN_MIN_DIVISION = 24;

	public readonly ushort RawValue;

	public TimeDivisionType Type => (TimeDivisionType)(RawValue >> 15);

	public ushort PPQN_TicksPerQuarterNote => RawValue; // Type bit is already 0

	public SMPTEFormat SMPTE_Format => (SMPTEFormat)(-(sbyte)(RawValue >> 8)); // Upper 8 bits, negated
	public byte SMPTE_TicksPerFrame => (byte)RawValue; // Lower 8 bits

	public TimeDivisionValue(ushort rawValue)
	{
		RawValue = rawValue;
	}

	public static TimeDivisionValue CreatePPQN(ushort ticksPerQuarterNote)
	{
		return new TimeDivisionValue(ticksPerQuarterNote);
	}
	public static TimeDivisionValue CreateSMPTE(SMPTEFormat format, byte ticksPerFrame)
	{
		ushort rawValue = (ushort)((-(sbyte)format) << 8);
		rawValue |= ticksPerFrame;

		return new TimeDivisionValue(rawValue);
	}

	public bool IsValid()
	{
		if (Type == TimeDivisionType.PPQN)
		{
			return PPQN_TicksPerQuarterNote >= PPQN_MIN_DIVISION;
		}

		// SMPTE
		return SMPTE_Format is SMPTEFormat.Smpte24 or SMPTEFormat.Smpte25 or SMPTEFormat.Smpte30Drop or SMPTEFormat.Smpte30;
	}

	public override string ToString()
	{
		switch (Type)
		{
			case TimeDivisionType.PPQN: return string.Format("PPQN [TicksPerQuarterNote: {0}]", PPQN_TicksPerQuarterNote);
			case TimeDivisionType.SMPTE: return string.Format("SMPTE [Format: {0}, TicksPerFrame: {1}]", SMPTE_Format, SMPTE_TicksPerFrame);
		}
		return string.Format("INVALID [0x{0:X4}]", RawValue);
	}
}
