using UnityEngine;

/// <summary>
/// Public API for loading and unloading levels. Composes
/// <see cref="LevelLoader"/> (disk/JSON) with <see cref="LevelBuilder"/>
/// (runtime state) and owns the "currently active level" handle so other
/// systems (bootstrapper, editor reload menu, level select UI) can ask one
/// object "what am I playing?".
///
/// The runner is the one place that publishes lifecycle events on the
/// <see cref="IEventBus"/>. Keeping the publishes here (rather than inside
/// <see cref="LevelBuilder"/>) means the builder stays a pure "take payload,
/// spawn stuff" worker and the runner owns sequencing: teardown → publish
/// ended → load → publish started.
///
/// Lifecycle side effects happen synchronously — fine at mobile scales and
/// much easier to reason about than an async pipeline would be here.
/// </summary>
public sealed class LevelRunner
{
    private readonly LevelLoader loader;
    private readonly LevelBuilder builder;
    private readonly IEventBus bus;

    /// <summary>The level currently loaded, or null if none.</summary>
    public LevelConfig Current { get; private set; }

    public LevelRunner(LevelLoader loader, LevelBuilder builder, IEventBus bus)
    {
        this.loader = loader;
        this.builder = builder;
        this.bus = bus;
    }

    /// <summary>
    /// Tears down any previously loaded level and builds the given one.
    /// Safe to call with the same config to reload in-place. Publishes
    /// <see cref="LevelStartedEvent"/> on success so the timer, evaluators
    /// and HUD all initialize together.
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
            bus.Publish(new LevelStartedEvent(payload.Id ?? config.name, payload.TimeLimit));
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
    /// Unloads the current level. Publishes <see cref="LevelEndedEvent"/>
    /// after teardown so subscribers can reset per-level caches.
    /// </summary>
    public void Unload()
    {
        var previousId = Current != null ? Current.name : string.Empty;
        builder.Teardown();
        Current = null;
        if (!string.IsNullOrEmpty(previousId))
            bus.Publish(new LevelEndedEvent(previousId));
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
