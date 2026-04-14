using UnityEngine;
using VContainer;

/// <summary>
/// Scene-owned kickoff component. On <c>Start</c> it asks the injected
/// <see cref="ILevelStartupStrategy"/> for the level to load and hands it
/// to the <see cref="LevelRunner"/>. Swapping the strategy (progression,
/// last-played, specific debug level) requires no changes here.
///
/// Dependencies are pushed in by VContainer via <see cref="Inject"/>; the
/// LifetimeScope registers this component via <c>RegisterComponentInHierarchy</c>.
/// Keeping the bootstrapper a MonoBehaviour (rather than a pure
/// <c>IStartable</c>) also lets the <c>BlockFlow → Reload Current Level</c>
/// editor menu discover the instance with <c>FindFirstObjectByType</c>.
/// </summary>
public sealed class GameplayBootstrapper : MonoBehaviour
{
    [SerializeField, Tooltip("Used by the default ProgressionOrFallbackStartupStrategy when no LevelCatalog is assigned.")]
    private LevelConfig fallbackLevel;

    public LevelConfig FallbackLevel => fallbackLevel;

    private LevelRunner runner;
    private LevelProgressionService progression;
    private ILevelStartupStrategy startupStrategy;

    [Inject]
    public void Construct(LevelRunner runner, LevelProgressionService progression, ILevelStartupStrategy startupStrategy)
    {
        this.runner = runner;
        this.progression = progression;
        this.startupStrategy = startupStrategy;
    }

    private void Start()
    {
        if (runner == null)
        {
            Debug.LogError("[GameplayBootstrapper] LevelRunner was not injected. Is this object inside a LifetimeScope?");
            return;
        }

        var level = startupStrategy != null ? startupStrategy.ResolveStartingLevel() : null;
        if (level == null)
        {
            Debug.LogWarning("[GameplayBootstrapper] No level to load (strategy returned null).");
            return;
        }

        runner.Load(level);
    }

    /// <summary>Reloads whichever level the runner currently has active. Editor menu hook.</summary>
    public void ReloadCurrent()
    {
        if (runner == null) return;
        runner.Reload();
    }

    /// <summary>Direct "jump to this level" for debug / level select shortcuts.</summary>
    public void Load(LevelConfig config)
    {
        if (runner == null) return;
        runner.Load(config);
    }

    /// <summary>
    /// Advances progression and loads the new current level. Returns whether
    /// the advance actually produced a loadable level.
    /// </summary>
    public bool LoadNext()
    {
        if (runner == null || progression == null) return false;
        progression.AdvanceToNext();
        var next = progression.Current;
        if (next == null) return false;
        runner.Load(next);
        return true;
    }
}
