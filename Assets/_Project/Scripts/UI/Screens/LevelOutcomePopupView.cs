using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Shows the Win or Lose popup panel when the corresponding bus event fires
/// and wires Restart / Next buttons to the <see cref="LevelRunner"/> and
/// <see cref="LevelProgressionService"/>. Hides both panels on
/// <see cref="LevelStartedEvent"/> so reloading a level dismisses any
/// leftover popup.
///
/// One component covers both outcomes because they share the same wiring
/// shape and it keeps the scene hierarchy flat. Split into separate popups
/// later if either needs meaningfully different behavior.
///
/// <para>
/// The win panel's Next button is hidden whenever the progression service
/// reports no next level — that's the cheapest way to communicate "you
/// finished the last level" without adding extra state or popups.
/// </para>
/// </summary>
public sealed class LevelOutcomePopupView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField, Tooltip("Panel shown on LevelWonEvent. Contains the restart and (optional) next buttons.")]
    private GameObject winPanel;

    [SerializeField, Tooltip("Panel shown on LevelLostEvent. Contains the restart button.")]
    private GameObject losePanel;

    [Header("Buttons")]
    [SerializeField, Tooltip("Reloads the current level from the Win panel.")]
    private Button winRestartButton;

    [SerializeField, Tooltip("Advances progression and loads the next level from the Win panel.")]
    private Button winNextButton;

    [SerializeField, Tooltip("Reloads the current level from the Lose panel.")]
    private Button loseRestartButton;

    private IEventBus bus;
    private LevelRunner runner;
    private LevelProgressionService progression;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    [Inject]
    public void Construct(IEventBus bus, LevelRunner runner, LevelProgressionService progression)
    {
        this.bus = bus;
        this.runner = runner;
        this.progression = progression;

        subs.Add(bus.Subscribe<LevelWonEvent>(_     => ShowWin()));
        subs.Add(bus.Subscribe<LevelLostEvent>(_    => Show(losePanel)));
        subs.Add(bus.Subscribe<LevelStartedEvent>(_ => HideAll()));

        if (winRestartButton  != null) winRestartButton .onClick.AddListener(OnRestart);
        if (loseRestartButton != null) loseRestartButton.onClick.AddListener(OnRestart);
        if (winNextButton     != null) winNextButton    .onClick.AddListener(OnNext);

        HideAll();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();

        if (winRestartButton  != null) winRestartButton .onClick.RemoveListener(OnRestart);
        if (loseRestartButton != null) loseRestartButton.onClick.RemoveListener(OnRestart);
        if (winNextButton     != null) winNextButton    .onClick.RemoveListener(OnNext);
    }

    // ---------- show / hide ----------

    private void ShowWin()
    {
        Show(winPanel);
        if (winNextButton != null)
            winNextButton.gameObject.SetActive(progression != null && progression.HasNext);
    }

    private void Show(GameObject panel)
    {
        HideAll();
        if (panel != null) panel.SetActive(true);
    }

    private void HideAll()
    {
        if (winPanel  != null) winPanel .SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ---------- button handlers ----------

    private void OnRestart()
    {
        HideAll();
        runner?.Reload();
    }

    private void OnNext()
    {
        HideAll();
        if (progression == null || runner == null) return;
        if (progression.AdvanceToNext())
            runner.Load(progression.Current);
        else
            runner.Reload();
    }
}
