using UnityEngine;
using VContainer;

// MonoBehaviour (rather than IStartable) so the editor Reload menu can FindFirstObjectByType it.
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

    public void ReloadCurrent()
    {
        if (runner == null) return;
        runner.Reload();
    }

    public void Load(LevelConfig config)
    {
        if (runner == null) return;
        runner.Load(config);
    }

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
