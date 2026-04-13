using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit pause popup. Defers element queries to OnEnable
/// to ensure UIDocument visual tree is ready.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class PausePopupView : MonoBehaviour
{
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement panel;
    private Button resumeBtn;
    private Button restartBtn;
    private Button homeBtn;
    private bool uiReady;

    private IEventBus bus;
    private LevelRunner runner;
    private GameStateService state;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    [Inject]
    public void Construct(IEventBus bus, LevelRunner runner, GameStateService state)
    {
        this.bus = bus;
        this.runner = runner;
        this.state = state;

        subs.Add(bus.Subscribe<LevelStartedEvent>(_ => HideImmediate()));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => HideImmediate()));
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

        resumeBtn  = ve.Q<Button>("ui_popup_pause_btn_resume");
        restartBtn = ve.Q<Button>("ui_popup_pause_btn_restart");
        homeBtn    = ve.Q<Button>("ui_popup_pause_btn_home");

        if (resumeBtn != null)  resumeBtn.clicked  += OnResume;
        if (restartBtn != null) restartBtn.clicked += OnRestart;
        if (homeBtn != null)    homeBtn.clicked    += OnHome;

        uiReady = root != null;
        HideImmediate();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();

        if (resumeBtn != null)  resumeBtn.clicked  -= OnResume;
        if (restartBtn != null) restartBtn.clicked -= OnRestart;
        if (homeBtn != null)    homeBtn.clicked    -= OnHome;
    }

    public void Show()
    {
        if (state == null || state.Current != GamePhase.Playing) return;
        if (!uiReady) InitUI();
        state.RequestPause();
        UIToolkitPopupAnimator.AnimateShow(root, overlay, panel);
    }

    private void HideImmediate()
    {
        UIToolkitPopupAnimator.HideImmediate(root);
    }

    private void OnResume()
    {
        UIToolkitPopupAnimator.AnimateHide(root, overlay, panel, 0.25f,
            () => state?.RequestResume());
    }

    private void OnRestart()
    {
        UIToolkitPopupAnimator.AnimateHide(root, overlay, panel, 0.25f, () =>
        {
            state?.RequestResume();
            runner?.Reload();
        });
    }

    private void OnHome()
    {
        UIToolkitPopupAnimator.AnimateHide(root, overlay, panel, 0.25f, () =>
        {
            state?.RequestResume();
            var levelMap = FindObjectOfType<LevelMapScreen>(true);
            if (levelMap != null) levelMap.ShowMap();
        });
    }
}
