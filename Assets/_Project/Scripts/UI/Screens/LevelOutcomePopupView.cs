using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit win/lose popup.
/// Win: 1s delay, trophy icon, stars (big bouncy animation), Next/Restart/Home.
/// Lose: same structure, stopwatch icon, Retry/Home.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class LevelOutcomePopupView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Texture2D trophyIcon;
    [SerializeField] private Texture2D stopwatchIcon;
    [SerializeField] private Texture2D starSprite;

    private VisualElement winRoot, winOverlay, winPanel;
    private VisualElement loseRoot, loseOverlay, losePanel;
    private VisualElement winIcon, loseIcon;
    private Label winTitle, loseTitle;
    private VisualElement[] stars = new VisualElement[3];
    private Button winNextBtn, winRestartBtn, winHomeBtn;
    private Button loseRetryBtn, loseHomeBtn;
    private bool uiReady;

    private IEventBus bus;
    private LevelRunner runner;
    private LevelProgressionService progression;
    private CountdownTimer timer;
    private ISceneLoader sceneLoader;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    [Inject]
    public void Construct(IEventBus bus, LevelRunner runner,
        LevelProgressionService progression, CountdownTimer timer, ISceneLoader sceneLoader,
        PopupAnimationConfig popupAnimation)
    {
        this.bus = bus;
        this.runner = runner;
        this.progression = progression;
        this.timer = timer;
        this.sceneLoader = sceneLoader;
        UIToolkitPopupAnimator.Config = popupAnimation;

        subs.Add(bus.Subscribe<LevelWonEvent>(_     => OnWon()));
        subs.Add(bus.Subscribe<LevelLostEvent>(e    => ShowLose(e)));
        subs.Add(bus.Subscribe<LevelStartedEvent>(_ => HideAll()));
    }

    private void OnEnable()
    {
        if (!uiReady) InitUI();
    }

    private void InitUI()
    {
        var root = GetFullscreenRoot();
        if (root == null) return;

        // Win
        winRoot    = root.Q("ui_popup_win_root");
        winOverlay = root.Q("ui_popup_win_overlay");
        winPanel   = root.Q("ui_popup_win_panel");
        winIcon    = root.Q("ui_popup_win_icon");
        winTitle   = root.Q<Label>("ui_popup_win_title_value");
        winNextBtn    = root.Q<Button>("ui_popup_win_btn_next");
        winRestartBtn = root.Q<Button>("ui_popup_win_btn_restart");
        winHomeBtn    = root.Q<Button>("ui_popup_win_btn_home");

        stars[0] = root.Q("ui_popup_win_star_1");
        stars[1] = root.Q("ui_popup_win_star_2");
        stars[2] = root.Q("ui_popup_win_star_3");

        // Lose
        loseRoot    = root.Q("ui_popup_lose_root");
        loseOverlay = root.Q("ui_popup_lose_overlay");
        losePanel   = root.Q("ui_popup_lose_panel");
        loseIcon    = root.Q("ui_popup_lose_icon");
        loseTitle   = root.Q<Label>("ui_popup_lose_title_value");
        loseRetryBtn = root.Q<Button>("ui_popup_lose_btn_retry");
        loseHomeBtn  = root.Q<Button>("ui_popup_lose_btn_home");

        // Wire buttons
        if (winNextBtn != null)    winNextBtn.clicked    += OnNext;
        if (winRestartBtn != null) winRestartBtn.clicked += OnRestart;
        if (winHomeBtn != null)    winHomeBtn.clicked    += OnHome;
        if (loseRetryBtn != null)  loseRetryBtn.clicked  += OnRestart;
        if (loseHomeBtn != null)   loseHomeBtn.clicked   += OnHome;

        // Set icon sprites
        if (winIcon != null && trophyIcon != null)
            winIcon.style.backgroundImage = new StyleBackground(trophyIcon);
        if (loseIcon != null && stopwatchIcon != null)
            loseIcon.style.backgroundImage = new StyleBackground(stopwatchIcon);

        // Set star sprites
        foreach (var star in stars)
        {
            if (star != null && starSprite != null)
                star.style.backgroundImage = new StyleBackground(starSprite);
        }

        uiReady = winRoot != null;
        HideAll();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();

        if (winNextBtn != null)    winNextBtn.clicked    -= OnNext;
        if (winRestartBtn != null) winRestartBtn.clicked -= OnRestart;
        if (winHomeBtn != null)    winHomeBtn.clicked    -= OnHome;
        if (loseRetryBtn != null)  loseRetryBtn.clicked  -= OnRestart;
        if (loseHomeBtn != null)   loseHomeBtn.clicked   -= OnHome;
    }

    // ── Win: 1 second delay before popup ──
    private void OnWon()
    {
        if (!uiReady) InitUI();
        int starCount = CalculateStars();

        // Advance progression immediately so Home/Restart also get the new level
        progression?.AdvanceToNext();

        // 1 second celebration delay
        Tween.Delay(1f, () => ShowWin(starCount));
    }

    private void EnsureFullscreen() => GetFullscreenRoot();

    private VisualElement GetFullscreenRoot()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return null;
        var tc = doc.rootVisualElement;
        if (tc == null) return null;
        tc.style.flexGrow = 1;
        tc.style.position = Position.Absolute;
        tc.style.left = 0;
        tc.style.top = 0;
        tc.style.right = 0;
        tc.style.bottom = 0;
        return tc;
    }

    private void ShowWin(int starCount)
    {
        EnsureFullscreen();
        HideAll();
        SetStarStates(starCount);

        if (winNextBtn != null)
            winNextBtn.style.display = DisplayStyle.Flex;

        // Hide Restart on win — only Next and Home
        if (winRestartBtn != null)
            winRestartBtn.style.display = DisplayStyle.None;

        UIToolkitPopupAnimator.AnimateShow(winRoot, winOverlay, winPanel, 0.45f,
            () => AnimateStarsBouncy(starCount));
    }

    private void ShowLose(LevelLostEvent evt)
    {
        if (!uiReady) InitUI();
        EnsureFullscreen();
        HideAll();

        if (loseTitle != null)
            loseTitle.text = evt.Reason == LevelLoseReason.TimerExpired
                ? "Time's Up!" : "No Moves Left!";

        UIToolkitPopupAnimator.AnimateShow(loseRoot, loseOverlay, losePanel);
    }

    private int CalculateStars()
    {
        if (timer == null || timer.Total <= 0f) return 3;
        float ratio = timer.Remaining / timer.Total;
        if (ratio > 0.66f) return 3;
        if (ratio > 0.33f) return 2;
        return 1;
    }

    private void SetStarStates(int count)
    {
        for (int i = 0; i < 3; i++)
        {
            if (stars[i] == null) continue;
            stars[i].RemoveFromClassList("star-filled");
            stars[i].RemoveFromClassList("star-empty");
            stars[i].AddToClassList(i < count ? "star-filled" : "star-empty");
            stars[i].transform.scale = Vector3.zero;
            stars[i].style.opacity = 0f;
        }
    }

    /// <summary>Big bouncy star animation with overshoot and slight rotation.</summary>
    private void AnimateStarsBouncy(int count)
    {
        for (int i = 0; i < 3; i++)
        {
            if (stars[i] == null) continue;
            bool filled = i < count;
            float delay = 0.2f + i * 0.25f;
            int idx = i;

            Tween.Delay(delay, () =>
            {
                if (filled)
                {
                    // Big bouncy: overshoot to 1.3 then settle to 1.0
                    stars[idx].style.opacity = 1f;
                    Tween.Custom(0f, 1.35f, 0.3f, val =>
                        stars[idx].transform.scale = new Vector3(val, val, 1f),
                        ease: Ease.OutQuad);

                    Tween.Delay(0.3f, () =>
                    {
                        Tween.Custom(1.35f, 1f, 0.2f, val =>
                            stars[idx].transform.scale = new Vector3(val, val, 1f),
                            ease: Ease.InOutQuad);
                    });

                    // Slight rotation wobble
                    float startRot = idx == 1 ? 0f : (idx == 0 ? -12f : 12f);
                    Tween.Custom(startRot, 0f, 0.4f, val =>
                        stars[idx].transform.rotation = Quaternion.Euler(0, 0, val),
                        ease: Ease.OutBack);
                }
                else
                {
                    stars[idx].style.opacity = 0.25f;
                    Tween.Custom(0f, 1f, 0.25f, val =>
                        stars[idx].transform.scale = new Vector3(val, val, 1f),
                        ease: Ease.OutQuad);
                }
            });
        }
    }

    private void HideAll()
    {
        UIToolkitPopupAnimator.HideImmediate(winRoot);
        UIToolkitPopupAnimator.HideImmediate(loseRoot);
    }

    private void OnRestart()
    {
        var activeRoot = winRoot != null && !winRoot.ClassListContains("hidden") ? winRoot : loseRoot;
        var activeOverlay = activeRoot == winRoot ? winOverlay : loseOverlay;
        var activePanel = activeRoot == winRoot ? winPanel : losePanel;

        UIToolkitPopupAnimator.AnimateHide(activeRoot, activeOverlay, activePanel, 0.25f,
            () => runner?.Reload());
    }

    private void OnNext()
    {
        UIToolkitPopupAnimator.AnimateHide(winRoot, winOverlay, winPanel, 0.25f, () =>
        {
            // Progression already advanced in OnWon; just load the new current level
            if (runner != null && progression != null)
                runner.Load(progression.Current);
        });
    }

    private void OnHome()
    {
        var activeRoot = winRoot != null && !winRoot.ClassListContains("hidden") ? winRoot : loseRoot;
        var activeOverlay = activeRoot == winRoot ? winOverlay : loseOverlay;
        var activePanel = activeRoot == winRoot ? winPanel : losePanel;

        UIToolkitPopupAnimator.AnimateHide(activeRoot, activeOverlay, activePanel, 0.25f, () =>
        {
            sceneLoader?.UnloadGameplay();
        });
    }
}
