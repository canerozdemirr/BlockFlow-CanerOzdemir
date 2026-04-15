using System;
using System.Collections.Generic;
using VContainer.Unity;

public sealed class GameStateService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public GamePhase Current { get; private set; } = GamePhase.Loading;
    public event Action<GamePhase> PhaseChanged;

    public GameStateService(IEventBus bus)
    {
        this.bus = bus;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<LevelStartedEvent>(_ => Set(GamePhase.Playing)));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => Set(GamePhase.Loading)));
        subs.Add(bus.Subscribe<LevelWonEvent>(_ => Set(GamePhase.Won)));
        subs.Add(bus.Subscribe<LevelLostEvent>(_ => Set(GamePhase.Lost)));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    public void RequestPause()
    {
        if (Current == GamePhase.Playing) Set(GamePhase.Paused);
    }

    public void RequestResume()
    {
        if (Current == GamePhase.Paused) Set(GamePhase.Playing);
    }

    private void Set(GamePhase phase)
    {
        if (Current == phase) return;
        Current = phase;
        PhaseChanged?.Invoke(phase);
    }
}
