using UnityEngine;
using VContainer;

/// <summary>
/// Scene-owned kickoff component. Holds a serialized reference to the level
/// that should load when the scene starts and asks the <see cref="LevelRunner"/>
/// to load it on <c>Start</c>. Keeping this a MonoBehaviour (rather than a
/// VContainer <c>IStartable</c>) gives us two things: the starting level is
/// editable in the inspector, and the editor reload menu can discover the
/// instance with <c>FindFirstObjectByType</c>.
///
/// Dependencies are pushed in by VContainer via the <see cref="Inject"/>
/// attribute on <see cref="Construct"/>; the LifetimeScope registers this
/// component via <c>RegisterComponentInHierarchy</c>.
/// </summary>
public sealed class GameplayBootstrapper : MonoBehaviour
{
    [SerializeField, Tooltip("Level loaded automatically when the scene starts.")]
    private LevelConfig startingLevel;

    private LevelRunner runner;

    [Inject]
    public void Construct(LevelRunner runner)
    {
        this.runner = runner;
    }

    private void Start()
    {
        if (runner == null)
        {
            Debug.LogError("[GameplayBootstrapper] LevelRunner was not injected. Is this object inside a LifetimeScope?");
            return;
        }

        if (startingLevel == null)
        {
            Debug.LogWarning("[GameplayBootstrapper] No starting level assigned; leaving the scene empty.");
            return;
        }

        runner.Load(startingLevel);
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

    /// <summary>Direct "jump to this level" for UI / debug shortcuts.</summary>
    public void Load(LevelConfig config)
    {
        if (runner == null) return;
        runner.Load(config);
    }
}
