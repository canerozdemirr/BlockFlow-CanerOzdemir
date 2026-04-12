/// <summary>
/// Fired by <see cref="CountdownTimer"/> every frame while the level is
/// running. HUD uses this to refresh its displayed timer; evaluators never
/// need the per-frame value.
/// </summary>
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

/// <summary>
/// Fired once when the countdown reaches zero. Picked up by
/// <see cref="LoseConditionEvaluator"/> to trigger a lose transition.
/// </summary>
public readonly struct TimerFinishedEvent
{
}
