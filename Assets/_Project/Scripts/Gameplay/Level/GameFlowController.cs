using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>
/// Bridges view-side request events to the gameplay-side state mutators.
/// Views publish intents (restart, advance, load current) instead of calling
/// <see cref="LevelRunner"/> or <see cref="LevelProgressionService"/> directly,
/// which keeps the UI assembly from knowing about flow orchestration.
/// </summary>
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
