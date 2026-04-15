public interface ILevelStartupStrategy
{
    LevelConfig ResolveStartingLevel();
}

public sealed class ProgressionOrFallbackStartupStrategy : ILevelStartupStrategy
{
    private readonly LevelProgressionService progression;
    private readonly LevelConfig fallback;

    public ProgressionOrFallbackStartupStrategy(LevelProgressionService progression, LevelConfig fallback)
    {
        this.progression = progression;
        this.fallback = fallback;
    }

    public LevelConfig ResolveStartingLevel()
    {
        var fromProgression = progression != null ? progression.Current : null;
        return fromProgression != null ? fromProgression : fallback;
    }
}
