public readonly struct LevelWonEvent
{
}

public enum LevelLoseReason
{
    TimerExpired,
    NoMovesRemaining
}

public readonly struct LevelLostEvent
{
    public readonly LevelLoseReason Reason;

    public LevelLostEvent(LevelLoseReason reason)
    {
        Reason = reason;
    }
}
