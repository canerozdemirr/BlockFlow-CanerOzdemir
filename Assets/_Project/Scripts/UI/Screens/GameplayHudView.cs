using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit gameplay HUD. Queries elements from the visual tree by name.
/// Timer fill uses <c>style.width</c> (percentage) to preserve rounded corners.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class GameplayHudView : MonoBehaviour
{
    private Label timerLabel;
    private VisualElement timerFill;
    private VisualElement timerPill;
    private Label levelNumberLabel;
    private Button pauseButton;

    private IEventBus bus;
    private LevelProgressionService progression;
    private readonly List<IDisposable> subs = new List<IDisposable>();
    private int lastDisplayedSecond = -1;

    private static readonly Color ColFull    = new Color(0.30f, 0.75f, 1.00f);
    private static readonly Color ColWarning = new Color(1.00f, 0.80f, 0.15f);
    private static readonly Color ColDanger  = new Color(1.00f, 0.30f, 0.20f);

    private bool uiReady;

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

        timerLabel       = root.Q<Label>("ui_hud_timer_text_value");
        timerFill        = root.Q("ui_hud_timer_pill_fill");
        timerPill        = root.Q("ui_hud_timer_pill");
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
        var pausePopup = FindObjectOfType<PausePopupView>(true);
        if (pausePopup != null) pausePopup.Show();
    }

    private void OnLevelStarted(LevelStartedEvent evt)
    {
        if (levelNumberLabel != null && progression != null)
            levelNumberLabel.text = (progression.CurrentIndex + 1).ToString();

        SetFill(1f);
    }

    private void OnTick(TimerTickEvent evt)
    {
        if (timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(evt.Remaining);
            if (seconds != lastDisplayedSecond)
            {
                lastDisplayedSecond = seconds;
                int minutes = seconds / 60;
                int rest = seconds % 60;
                timerLabel.text = $"{minutes:00}:{rest:00}";
            }
        }

        if (evt.Total > 0f)
        {
            float ratio = evt.Remaining / evt.Total;
            SetFill(ratio);
            UpdateFillColor(ratio);
        }
    }

    private void SetFill(float ratio)
    {
        if (timerFill == null || timerPill == null) return;
        // Drive fill by adjusting the right inset. At ratio=1 right=8px, at ratio=0 right=~100%.
        float pillWidth = timerPill.resolvedStyle.width;
        if (pillWidth <= 0) return;
        float padding = 8f;
        float maxFillWidth = pillWidth - padding * 2;
        float targetRight = padding + maxFillWidth * (1f - Mathf.Clamp01(ratio));
        timerFill.style.right = targetRight;
    }

    private void UpdateFillColor(float ratio)
    {
        if (timerFill == null) return;

        Color color;
        if (ratio > 0.5f)
            color = ColFull;
        else if (ratio > 0.2f)
            color = Color.Lerp(ColWarning, ColFull, (ratio - 0.2f) / 0.3f);
        else
            color = Color.Lerp(ColDanger, ColWarning, ratio / 0.2f);

        timerFill.style.backgroundColor = new StyleColor(color);
    }

    private void ResetDisplay()
    {
        lastDisplayedSecond = -1;
        if (timerLabel != null) timerLabel.text = string.Empty;
        SetFill(1f);
    }
}
