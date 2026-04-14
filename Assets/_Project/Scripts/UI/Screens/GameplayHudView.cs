using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit gameplay HUD. Single strip background with level badge,
/// timer text (no fill bar), and pause button. Fixed width, centered.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class GameplayHudView : MonoBehaviour
{
    private Label timerLabel;
    private Label levelNumberLabel;
    private Button pauseButton;
    private bool uiReady;

    private IEventBus bus;
    private LevelProgressionService progression;
    private readonly List<IDisposable> subs = new List<IDisposable>();
    private int lastDisplayedSecond = -1;

    [Inject]
    public void Construct(IEventBus bus, LevelProgressionService progression)
    {
        this.bus = bus;
        this.progression = progression;

        subs.Add(bus.Subscribe<TimerTickEvent>(OnTick));
        subs.Add(bus.Subscribe<LevelStartedEvent>(OnLevelStarted));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => ResetDisplay()));
    }

    private void OnEnable()
    {
        if (!uiReady) InitUI();
    }

    private void InitUI()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var root = doc.rootVisualElement;
        if (root == null) return;

        root.style.flexGrow = 1;
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.style.right = 0;
        root.style.bottom = 0;

        timerLabel       = root.Q<Label>("ui_hud_timer_text_value");
        levelNumberLabel = root.Q<Label>("ui_hud_level_number_value");
        pauseButton      = root.Q<Button>("ui_hud_btn_pause");

        if (pauseButton != null)
            pauseButton.clicked += OnPauseClicked;

        uiReady = timerLabel != null;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();

        if (pauseButton != null)
            pauseButton.clicked -= OnPauseClicked;
    }

    private void OnPauseClicked()
    {
        bus?.Publish(new PauseRequestedEvent());
    }

    private void OnLevelStarted(LevelStartedEvent evt)
    {
        if (!uiReady) InitUI();

        if (levelNumberLabel != null && progression != null)
            levelNumberLabel.text = progression.LevelNumber.ToString();
    }

    private void OnTick(TimerTickEvent evt)
    {
        if (timerLabel == null) return;

        int seconds = Mathf.CeilToInt(evt.Remaining);
        if (seconds == lastDisplayedSecond) return;
        lastDisplayedSecond = seconds;

        int minutes = seconds / 60;
        int rest = seconds % 60;
        timerLabel.text = $"{minutes:00}:{rest:00}";
    }

    private void ResetDisplay()
    {
        lastDisplayedSecond = -1;
        if (timerLabel != null) timerLabel.text = string.Empty;
    }
}
