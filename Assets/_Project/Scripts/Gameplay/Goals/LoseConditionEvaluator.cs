using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>
/// Subscribes to <see cref="TimerFinishedEvent"/> and publishes
/// <see cref="LevelLostEvent"/> with <see cref="LevelLoseReason.TimerExpired"/>.
/// Kept as its own evaluator even though it's currently a one-liner so
/// deadlock detection or special-block fail states can land here without
/// touching anything else.
/// </summary>
public sealed class LoseConditionEvaluator : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly GameStateService state;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public LoseConditionEvaluator(IEventBus bus, GameStateService state)
    {
        this.bus = bus;
        this.state = state;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<TimerFinishedEvent>(OnTimerFinished));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    private void OnTimerFinished(TimerFinishedEvent _)
    {
        if (state.Current != GamePhase.Playing) return;
        bus.Publish(new LevelLostEvent(LevelLoseReason.TimerExpired));
    }
}
