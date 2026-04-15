using System;
using System.Collections.Generic;
using VContainer.Unity;

// Bridges view-side request events to flow mutators so the UI assembly
// doesn't need to know about LevelRunner/LevelProgressionService directly.
public sealed class GameFlowController : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelRunner runner;
    private readonly LevelProgressionService progression;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public GameFlowController(IEventBus bus, LevelRunner runner, LevelProgressionService progression)
    {
        this.bus = bus;
        this.runner = runner;
        this.progression = progression;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<LevelRestartRequestedEvent>(_ => runner?.Reload()));
        subs.Add(bus.Subscribe<LevelAdvanceRequestedEvent>(_ => progression?.AdvanceToNext()));
        subs.Add(bus.Subscribe<LevelLoadCurrentRequestedEvent>(_ =>
        {
            if (runner != null && progression != null && progression.Current != null)
                runner.Load(progression.Current);
        }));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }
}
