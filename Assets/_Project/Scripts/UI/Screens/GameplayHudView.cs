using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// Minimal in-game HUD. Subscribes to <see cref="TimerTickEvent"/> on the
/// <see cref="IEventBus"/> and renders the countdown into a TMP label.
/// Deliberately tiny — the rest of the HUD (score, combo, hint button) can
/// be added by expanding this component or splitting into MVP presenters
/// when the scope calls for it.
///
/// Registered via <c>RegisterComponentInHierarchy</c> so VContainer resolves
/// the bus into <see cref="Construct"/> automatically on scene start.
/// </summary>
public sealed class GameplayHudView : MonoBehaviour
{
    [SerializeField, Tooltip("Label showing the remaining time in mm:ss.")]
    private TMP_Text timerText;

    private IEventBus bus;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    // Only rebuilds the display string when the integer second changes;
    // saves a handful of allocations per frame.
    private int lastDisplayedSecond = -1;

    [Inject]
    public void Construct(IEventBus bus)
    {
        this.bus = bus;
        subs.Add(bus.Subscribe<TimerTickEvent>(OnTick));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => ResetLabel()));
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    private void OnTick(TimerTickEvent evt)
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(evt.Remaining);
        if (seconds == lastDisplayedSecond) return;
        lastDisplayedSecond = seconds;

        int minutes = seconds / 60;
        int rest    = seconds % 60;
        timerText.text = $"{minutes}:{rest:00}";
    }

    private void ResetLabel()
    {
        lastDisplayedSecond = -1;
        if (timerText != null) timerText.text = string.Empty;
    }
}
