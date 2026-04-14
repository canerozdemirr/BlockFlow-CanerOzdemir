/// <summary>
/// Resolves which level the <see cref="GameplayBootstrapper"/> should load on
/// scene start. Abstracting this lets us swap progression for "last played",
/// "specific debug level", or test harness fixtures without touching the
/// bootstrapper.
/// </summary>
public interface ILevelStartupStrategy
{
    LevelConfig ResolveStartingLevel();
}

/// <summary>
/// Default strategy: prefer <see cref="LevelProgressionService.Current"/>,
/// falling back to a hand-picked designer-assigned level when no catalog is
/// wired. Kept dumb on purpose — any scheduling logic goes in a dedicated
/// strategy, not here.
/// </summary>
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
