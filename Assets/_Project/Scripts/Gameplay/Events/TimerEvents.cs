public readonly struct TimerTickEvent
{
    public readonly float Remaining;
    public readonly float Total;

    public TimerTickEvent(float remaining, float total)
    {
        Remaining = remaining;
        Total = total;
    }
}

public readonly struct TimerFinishedEvent
{
}
