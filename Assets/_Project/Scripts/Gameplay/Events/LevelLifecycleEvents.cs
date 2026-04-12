/// <summary>
/// Fired by <see cref="LevelRunner"/> immediately after a level is fully
/// built and its context is bound. Timer, evaluators, and HUD all use this
/// as their "reset everything and start" signal. Payload carries the things
/// most downstream systems need so they don't have to query the context.
/// </summary>
public readonly struct LevelStartedEvent
{
    public readonly string LevelId;
    public readonly float TimeLimit;

    public LevelStartedEvent(string levelId, float timeLimit)
    {
        LevelId = levelId;
        TimeLimit = timeLimit;
    }
}

/// <summary>
/// Fired by <see cref="LevelRunner"/> right after teardown. Services use
/// this to clear per-level caches (grinders, view registry, timer state).
/// </summary>
public readonly struct LevelEndedEvent
{
    public readonly string LevelId;

    public LevelEndedEvent(string levelId)
    {
        LevelId = levelId;
    }
}
