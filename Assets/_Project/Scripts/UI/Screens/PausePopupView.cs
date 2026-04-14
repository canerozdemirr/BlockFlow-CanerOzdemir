using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Settings-style pause panel. Slides down from top.
/// Sets Time.timeScale to 0 on open, 1 on close.
/// Contains Sound/Music/Haptic toggles and HOME button.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class PausePopupView : MonoBehaviour
{
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement panel;
    private Button closeBtn;
    private Button homeBtn;
    private Toggle soundToggle;
    private Toggle musicToggle;
    private Toggle hapticToggle;
    private bool uiReady;

    private IEventBus bus;
    private LevelRunner runner;
    private GameStateService state;
    private ISceneLoader sceneLoader;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    private const float SlideDuration = 0.35f;

    [Inject]
    public void Construct(IEventBus bus, LevelRunner runner, GameStateService state, ISceneLoader sceneLoader,
        PopupAnimationConfig popupAnimation)
    {
        this.bus = bus;
        this.runner = runner;
        this.state = state;
        this.sceneLoader = sceneLoader;
        UIToolkitPopupAnimator.Config = popupAnimation;

        subs.Add(bus.Subscribe<LevelStartedEvent>(_ => HideImmediate()));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => HideImmediate()));
        subs.Add(bus.Subscribe<PauseRequestedEvent>(_ => Show()));
    }

    private void OnEnable()
    {
        if (!uiReady) InitUI();
    }

    private void InitUI()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var ve = doc.rootVisualElement;
        if (ve == null) return;

        ve.style.flexGrow = 1;
        ve.style.position = Position.Absolute;
        ve.style.left = 0;
        ve.style.top = 0;
        ve.style.right = 0;
        ve.style.bottom = 0;

        root    = ve.Q("ui_popup_pause_root");
        overlay = ve.Q("ui_popup_pause_overlay");
        panel   = ve.Q("ui_popup_pause_panel");

        closeBtn = ve.Q<Button>("ui_popup_pause_btn_close");
        homeBtn  = ve.Q<Button>("ui_popup_pause_btn_home");

        soundToggle  = ve.Q<Toggle>("ui_popup_pause_toggle_sound");
        musicToggle  = ve.Q<Toggle>("ui_popup_pause_toggle_music");
        hapticToggle = ve.Q<Toggle>("ui_popup_pause_toggle_haptic");

        if (closeBtn != null) closeBtn.clicked += OnClose;
        if (homeBtn != null)  homeBtn.clicked  += OnHome;

        if (hapticToggle != null)
        {
            hapticToggle.value = HapticsService.Enabled;
            hapticToggle.RegisterValueChangedCallback(evt => HapticsService.Enabled = evt.newValue);
        }

        // TODO: Wire sound/music toggles to AudioService when implemented

        uiReady = root != null;
        HideImmediate();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();

        if (closeBtn != null) closeBtn.clicked -= OnClose;
        if (homeBtn != null)  homeBtn.clicked  -= OnHome;
    }

    public void Show()
    {
        if (state == null || state.Current != GamePhase.Playing) return;
        if (!uiReady) InitUI();

        state.RequestPause();
        Time.timeScale = 0f;

        if (root == null) return;
        root.RemoveFromClassList("hidden");

        // Overlay fade in
        if (overlay != null)
        {
            overlay.style.opacity = 0f;
            Tween.Custom(0f, 1f, SlideDuration, val => overlay.style.opacity = val,
                ease: Ease.OutQuad, useUnscaledTime: true);
        }

        // Panel slides down from above
        if (panel != null)
        {
            panel.transform.position = new Vector3(0, -800f, 0);
            panel.style.opacity = 1f;
            Tween.Custom(-800f, 0f, SlideDuration, val =>
                panel.transform.position = new Vector3(0, val, 0),
                ease: Ease.OutBack, useUnscaledTime: true);
        }
    }

    private void HideImmediate()
    {
        UIToolkitPopupAnimator.HideImmediate(root);
        Time.timeScale = 1f;
    }

    private void AnimateHide(Action onComplete = null)
    {
        // Panel slides up
        if (panel != null)
        {
            Tween.Custom(0f, -800f, SlideDuration * 0.7f, val =>
                panel.transform.position = new Vector3(0, val, 0),
                ease: Ease.InBack, useUnscaledTime: true);
        }

        // Overlay fade out
        if (overlay != null)
        {
            Tween.Custom(1f, 0f, SlideDuration * 0.7f, val => overlay.style.opacity = val,
                ease: Ease.InQuad, useUnscaledTime: true);
        }

        Tween.Delay(SlideDuration * 0.7f, () =>
        {
            root?.AddToClassList("hidden");
            if (panel != null)
                panel.transform.position = Vector3.zero;
            Time.timeScale = 1f;
            onComplete?.Invoke();
        }, useUnscaledTime: true);
    }

    private void OnClose()
    {
        AnimateHide(() => state?.RequestResume());
    }

    private void OnHome()
    {
        AnimateHide(() =>
        {
            state?.RequestResume();
            sceneLoader?.UnloadGameplay();
        });
    }
}
