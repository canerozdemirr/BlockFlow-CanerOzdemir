using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Per-level countdown timer. Picks up its duration from
/// <see cref="LevelStartedEvent.TimeLimit"/>, ticks while the phase is
/// <see cref="GamePhase.Playing"/>, and publishes <see cref="TimerTickEvent"/>
/// every frame for the HUD plus a one-shot <see cref="TimerFinishedEvent"/>
/// on expiry.
///
/// Using an <see cref="ITickable"/> rather than a coroutine means:
/// <list type="bullet">
///   <item>No MonoBehaviour, no GameObject, no scene placement.</item>
///   <item>Pause is a single-line check against <see cref="GameStateService.Current"/>.</item>
///   <item>Zero allocations per frame (the event struct is value-typed).</item>
/// </list>
/// </summary>
public sealed class CountdownTimer : ITickable, IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly GameStateService state;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    private float total;
    private float remaining;
    private bool running;

    public float Remaining => remaining;
    public float Total => total;
    public bool IsRunning => running;

    public CountdownTimer(IEventBus bus, GameStateService state)
    {
        this.bus = bus;
        this.state = state;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<LevelStartedEvent>(OnLevelStarted));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => Stop()));
        subs.Add(bus.Subscribe<LevelWonEvent>(_ => Stop()));
        subs.Add(bus.Subscribe<LevelLostEvent>(_ => Stop()));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    public void Tick()
    {
        if (!running) return;
        if (state.Current != GamePhase.Playing) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            bus.Publish(new TimerTickEvent(0f, total));
            bus.Publish(new TimerFinishedEvent());
            return;
        }

        bus.Publish(new TimerTickEvent(remaining, total));
    }

    private void OnLevelStarted(LevelStartedEvent evt)
    {
        total = Mathf.Max(1f, evt.TimeLimit);
        remaining = total;
        running = true;
        bus.Publish(new TimerTickEvent(remaining, total));
    }

    private void Stop()
    {
        running = false;
    }
}
