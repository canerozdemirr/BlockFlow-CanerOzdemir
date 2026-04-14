using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Haptic feedback service using CandyCoded.HapticFeedback for native
/// iOS Core Haptics and Android vibration patterns.
///
/// Intensity levels:
///   Tap    → Selection feedback (lightest, drag pickup)
///   Bump   → Light impact (wall bump, grind complete)
///   Strong → Success/heavy (level win)
///
/// Respects an enabled flag that the pause settings toggle can control.
/// </summary>
public sealed class HapticsService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    private const string PrefKey = "haptics.enabled";
    private static bool enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;

    public static bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled == value) return;
            enabled = value;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

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

    /// <summary>Lightest feedback — drag pickup, selection.</summary>
    public void Tap()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
#endif
    }

    /// <summary>Medium feedback — wall bump, grind, lose.</summary>
    public void Bump()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.MediumFeedback();
#endif
    }

    /// <summary>Heavy feedback — level win.</summary>
    public void Strong()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.HeavyFeedback();
#endif
    }
}
