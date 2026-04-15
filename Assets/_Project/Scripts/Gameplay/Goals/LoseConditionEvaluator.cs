using System;
using System.Collections.Generic;
using VContainer.Unity;

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
