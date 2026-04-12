/// <summary>
/// Fired by <see cref="WinConditionEvaluator"/> the moment the board clears.
/// </summary>
public readonly struct LevelWonEvent
{
}

/// <summary>
/// Reason the player failed the level. The enum is deliberately small:
/// more entries will appear if / when we add deadlock detection or
/// special-block fail states.
/// </summary>
public enum LevelLoseReason
{
    TimerExpired,
    NoMovesRemaining
}

/// <summary>
/// Fired by <see cref="LoseConditionEvaluator"/> on any lose condition.
/// Carries the reason so the popup can tailor its message if it chooses.
/// </summary>
public readonly struct LevelLostEvent
{
    public readonly LevelLoseReason Reason;

    public LevelLostEvent(LevelLoseReason reason)
    {
        Reason = reason;
    }
}
