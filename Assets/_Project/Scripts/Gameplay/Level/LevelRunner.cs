using UnityEngine;

// The runner is the one place that publishes lifecycle events on the bus,
// so the builder stays a pure "take payload, spawn stuff" worker.
public sealed class LevelRunner
{
    private readonly LevelLoader loader;
    private readonly LevelBuilder builder;
    private readonly IEventBus bus;

    public LevelConfig Current { get; private set; }

    public LevelRunner(LevelLoader loader, LevelBuilder builder, IEventBus bus)
    {
        this.loader = loader;
        this.builder = builder;
        this.bus = bus;
    }

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
            // Clean partial state so next Load starts from a sane baseline.
            builder.Teardown();
            Current = null;
        }
    }

    // Publishes LevelEndedEvent BEFORE teardown so subscribers can still
    // access grid/views during cleanup.
    public void Unload()
    {
        var previousId = Current != null ? Current.name : string.Empty;
        if (!string.IsNullOrEmpty(previousId))
            bus.Publish(new LevelEndedEvent(previousId));
        builder.Teardown();
        Current = null;
    }

    public void Reload()
    {
        if (Current == null) return;
        var cached = Current;
        Unload();
        Load(cached);
    }
}
