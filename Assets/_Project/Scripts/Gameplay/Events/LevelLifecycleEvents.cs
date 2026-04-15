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

public readonly struct LevelEndedEvent
{
    public readonly string LevelId;

    public LevelEndedEvent(string levelId)
    {
        LevelId = levelId;
    }
}
