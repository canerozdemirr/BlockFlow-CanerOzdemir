using UnityEngine;
using VContainer;

/// <summary>
/// Scene-owned kickoff component. On <c>Start</c> it asks the
/// <see cref="LevelProgressionService"/> for the current level and hands
/// it to the <see cref="LevelRunner"/>. If no catalog is wired (progression
/// has nothing to return) it falls back to a hand-picked serialized level
/// so the scene still boots with something playable.
///
/// Dependencies are pushed in by VContainer via <see cref="Inject"/>; the
/// LifetimeScope registers this component via <c>RegisterComponentInHierarchy</c>.
/// Keeping the bootstrapper a MonoBehaviour (rather than a pure
/// <c>IStartable</c>) also lets the <c>BlockFlow → Reload Current Level</c>
/// editor menu discover the instance with <c>FindFirstObjectByType</c>.
/// </summary>
public sealed class GameplayBootstrapper : MonoBehaviour
{
    [SerializeField, Tooltip("Used only if no LevelCatalog is configured. The first boot otherwise resolves through LevelProgressionService.")]
    private LevelConfig fallbackLevel;

    private LevelRunner runner;
    private LevelProgressionService progression;

    [Inject]
    public void Construct(LevelRunner runner, LevelProgressionService progression)
    {
        this.runner = runner;
        this.progression = progression;
    }

    private void Start()
    {
        if (runner == null)
        {
            Debug.LogError("[GameplayBootstrapper] LevelRunner was not injected. Is this object inside a LifetimeScope?");
            return;
        }

        var level = ResolveStartingLevel();
        if (level == null)
        {
            Debug.LogWarning("[GameplayBootstrapper] No level to load (no progression catalog and no fallback assigned).");
            return;
        }

        runner.Load(level);
    }

    /// <summary>
    /// Reloads whichever level the runner currently has active. Used by the
    /// <c>BlockFlow → Reload Current Level</c> editor menu.
    /// </summary>
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
    /// Advances the progression pointer and loads the new current level.
    /// No-op if the catalog is missing or the player is already on the
    /// last level. Returns whether the advance actually happened.
    /// </summary>
    public bool LoadNext()
    {
        if (runner == null || progression == null) return false;
        if (!progression.AdvanceToNext()) return false;
        var next = progression.Current;
        if (next == null) return false;
        runner.Load(next);
        return true;
    }

    // ---------- internals ----------

    private LevelConfig ResolveStartingLevel()
    {
        var fromProgression = progression != null ? progression.Current : null;
        return fromProgression != null ? fromProgression : fallbackLevel;
    }
}
