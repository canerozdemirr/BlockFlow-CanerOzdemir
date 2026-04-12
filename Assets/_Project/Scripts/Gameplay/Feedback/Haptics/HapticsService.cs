using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Minimal haptics wrapper. Subscribes to the highest-value bus events and
/// calls into <see cref="Handheld.Vibrate"/> on mobile targets. Unity's
/// built-in API is a single "vibrate" call with no intensity control, so
/// the <see cref="Tap"/>/<see cref="Bump"/>/<see cref="Strong"/> distinction
/// is aspirational today — swapping in a real iOS Core Haptics plugin or
/// Lofelt Nice Vibrations later is a drop-in change.
///
/// On desktop the methods are no-ops so the same service works in editor
/// without spamming anything.
/// </summary>
public sealed class HapticsService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public HapticsService(IEventBus bus)
    {
        this.bus = bus;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockDragStartedEvent>(_ => Tap()));
        subs.Add(bus.Subscribe<BlockBumpedWallEvent>(_  => Bump()));
        subs.Add(bus.Subscribe<BlockGroundEvent>(_      => Bump()));
        subs.Add(bus.Subscribe<LevelWonEvent>(_         => Strong()));
        subs.Add(bus.Subscribe<LevelLostEvent>(_        => Bump()));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    /// <summary>Short, low-intensity pulse for light confirmations (drag pickup).</summary>
    public void Tap() => Vibrate();

    /// <summary>Medium pulse for impact feedback (wall bump, grind).</summary>
    public void Bump() => Vibrate();

    /// <summary>Heavier pulse for high-intensity moments (win, lose).</summary>
    public void Strong() => Vibrate();

    private static void Vibrate()
    {
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }
}
