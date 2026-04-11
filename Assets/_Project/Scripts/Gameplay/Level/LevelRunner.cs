using UnityEngine;

/// <summary>
/// Public API for loading and unloading levels. Composes
/// <see cref="LevelLoader"/> (disk/JSON) with <see cref="LevelBuilder"/>
/// (runtime state) and owns the "currently active level" handle so other
/// systems (bootstrapper, editor reload menu, level select UI) can ask one
/// object "what am I playing?".
///
/// The runner deliberately does not inherit from MonoBehaviour or ITickable;
/// it is a plain service, constructed once via DI, stateless apart from the
/// current level reference. Lifecycle side effects (spawning, tearing down)
/// happen synchronously — fine at mobile scales and much easier to reason
/// about than an async pipeline would be here.
/// </summary>
public sealed class LevelRunner
{
    private readonly LevelLoader loader;
    private readonly LevelBuilder builder;

    /// <summary>The level currently loaded, or null if none.</summary>
    public LevelConfig Current { get; private set; }

    public LevelRunner(LevelLoader loader, LevelBuilder builder)
    {
        this.loader = loader;
        this.builder = builder;
    }

    /// <summary>
    /// Tears down any previously loaded level and builds the given one.
    /// Safe to call with the same config to reload in-place.
    /// </summary>
    public void Load(LevelConfig config)
    {
        if (config == null)
        {
            Debug.LogError("[LevelRunner] Cannot load a null LevelConfig.");
            return;
        }

        if (Current != null) Unload();

        var payload = loader.Load(config);
        if (payload == null) return;

        try
        {
            builder.Build(payload);
            Current = config;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelRunner] Build failed for '{config.name}': {e.Message}");
            // Clean up partial state so the next Load() starts from a sane baseline.
            builder.Teardown();
            Current = null;
        }
    }

    /// <summary>
    /// Unloads the current level. No-op if nothing is loaded.
    /// </summary>
    public void Unload()
    {
        builder.Teardown();
        Current = null;
    }

    /// <summary>
    /// Reloads whatever <see cref="Current"/> points at. Cheap dev iteration
    /// hook driven by the editor menu.
    /// </summary>
    public void Reload()
    {
        if (Current == null) return;
        var cached = Current;
        Unload();
        Load(cached);
    }
}
