using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

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

    public void Tap()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
#endif
    }

    public void Bump()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.MediumFeedback();
#endif
    }

    public void Strong()
    {
        if (!enabled) return;
#if UNITY_IOS || UNITY_ANDROID
        CandyCoded.HapticFeedback.HapticFeedback.HeavyFeedback();
#endif
    }
}
