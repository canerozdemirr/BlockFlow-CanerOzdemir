using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Level Map screen for the dedicated Level Map scene.
/// Vertical scrolling list of level nodes connected by path lines.
/// Current = green, completed = blue, locked = dark with lock icon.
/// Shows extra locked levels beyond the catalog to fill the map.
/// Auto-scrolls to current level on show.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class LevelMapScreen : MonoBehaviour
{
    private VisualElement root;
    private ScrollView scrollView;
    private VisualElement content;
    private Label levelLabel;
    private Button playBtn;
    private bool uiReady;

    private LevelProgressionService progression;
    private ISceneLoader sceneLoader;

    private readonly List<VisualElement> nodeElements = new List<VisualElement>();

    /// <summary>
    /// Total levels to display on the map. Shows extra locked levels
    /// beyond the catalog so the map feels full.
    /// </summary>
    private const int ExtraLockedLevels = 10;

    [Inject]
    public void Construct(LevelProgressionService progression, ISceneLoader sceneLoader)
    {
        this.progression = progression;
        this.sceneLoader = sceneLoader;
    }

    private void Start()
    {
        InitUI();
        if (sceneLoader != null) sceneLoader.OnGameplayUnloaded += Refresh;
    }

    private void OnEnable()
    {
        // When returning from gameplay, re-build the map
        if (progression != null && uiReady)
            BuildMap();
    }

    private void InitUI()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var ve = doc.rootVisualElement;
        if (ve == null) return;

        // Use GeometryChangedEvent to apply stretch AFTER layout resolves
        ve.RegisterCallback<GeometryChangedEvent>(OnTemplateContainerGeometryChanged);

        // Also apply immediately in case it already resolved
        ForceStretchTemplateContainer(ve);

        root       = ve.Q("ui_map_root");
        scrollView = ve.Q<ScrollView>("ui_map_scroll");
        content    = ve.Q("ui_map_content");
        levelLabel = ve.Q<Label>("ui_map_level_label_value");
        playBtn    = ve.Q<Button>("ui_map_btn_play");

        if (playBtn != null)
            playBtn.clicked += OnPlayClicked;

        // Lock the scroll — player cannot pan the map. The bottom (current)
        // node is snapped into view by ScrollToBottomWhenReady and stays there.
        // Hide the scrollbar chrome and block the viewport from receiving the
        // drag/wheel events that would otherwise move scrollOffset.
        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // ScrollView's internal drag/wheel handlers register on the scrollView
            // itself at TrickleDown. Swallow the raw input events before they
            // reach those handlers so the user cannot pan the map. We keep the
            // programmatic scrollOffset assignment (ScrollToBottomWhenReady) —
            // that doesn't go through the event pipeline.
            EventCallback<PointerDownEvent>  downCb  = evt => evt.StopImmediatePropagation();
            EventCallback<PointerMoveEvent>  moveCb  = evt => evt.StopImmediatePropagation();
            EventCallback<PointerUpEvent>    upCb    = evt => evt.StopImmediatePropagation();
            EventCallback<PointerCancelEvent> cancCb = evt => evt.StopImmediatePropagation();
            EventCallback<WheelEvent>        wheelCb = evt => evt.StopImmediatePropagation();

            scrollView.RegisterCallback(downCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(moveCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(upCb,    TrickleDown.TrickleDown);
            scrollView.RegisterCallback(cancCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(wheelCb, TrickleDown.TrickleDown);
        }

        uiReady = root != null && content != null;

        // Build immediately — layout will resolve in the same frame
        if (uiReady)
            BuildMap();
    }

    private void ForceStretchTemplateContainer(VisualElement ve)
    {
        ve.style.flexGrow = 1;
        ve.style.position = Position.Absolute;
        ve.style.left = 0;
        ve.style.top = 0;
        ve.style.right = 0;
        ve.style.bottom = 0;
        // Keep the root transparent to picks — child buttons still register.
        ve.pickingMode = PickingMode.Ignore;
    }

    private void OnTemplateContainerGeometryChanged(GeometryChangedEvent evt)
    {
        var ve = evt.target as VisualElement;
        if (ve == null) return;

        // Re-apply stretch every time geometry changes until it sticks
        if (ve.resolvedStyle.height <= 0)
        {
            ForceStretchTemplateContainer(ve);
        }
        else
        {
            // Layout resolved, rebuild map if not already done
            if (content != null && content.childCount == 0 && progression != null)
                BuildMap();
        }
    }

    private void OnDestroy()
    {
        if (playBtn != null)
            playBtn.clicked -= OnPlayClicked;
        if (sceneLoader != null) sceneLoader.OnGameplayUnloaded -= Refresh;
    }

    // ================================================================
    //  MAP BUILDER
    // ================================================================

    private void BuildMap()
    {
        if (!uiReady || progression == null) return;

        content.Clear();
        nodeElements.Clear();

        int currentLevelNum = progression.LevelNumber; // 1-based
        int nodesToShow = ExtraLockedLevels + 1; // current + locked above

        // Build top-to-bottom: highest future level at top, current at bottom.
        // offset 0 = current level (bottom), offset 1 = next, offset 2 = next+1, etc.
        for (int offset = nodesToShow - 1; offset >= 0; offset--)
        {
            int levelNum = currentLevelNum + offset;
            bool isCurrent = (offset == 0);

            var node = CreateNode(levelNum, isCurrent);
            content.Add(node);
            nodeElements.Add(node);

            // Path line below the node (except for the bottom-most = current)
            if (offset > 0)
            {
                var pathLine = new VisualElement();
                pathLine.AddToClassList("path-line");
                pathLine.AddToClassList("path-line-inactive");
                pathLine.pickingMode = PickingMode.Ignore;
                content.Add(pathLine);
            }
        }

        // Update level label
        if (levelLabel != null)
            levelLabel.text = $"Level {currentLevelNum}";

        // Auto-scroll to bottom (current level). A single-frame schedule fires
        // before USS/layout has resolved child heights — contentContainer.layout.height
        // reads 0 and the scroll snaps to the top. Hook GeometryChangedEvent on
        // the content so we jump the moment real heights are known, then detach.
        ScrollToBottomWhenReady();

        AnimateNodesIn();
    }

    /// <summary>
    /// Snaps the ScrollView to the bottom as soon as the content container's
    /// layout resolves. Uses GeometryChangedEvent because schedule.Execute fires
    /// the same frame — before USS flex math has produced real child heights.
    /// </summary>
    private void ScrollToBottomWhenReady()
    {
        if (scrollView == null) return;
        var container = scrollView.contentContainer;
        if (container == null) return;

        void OnGeometry(GeometryChangedEvent evt)
        {
            if (container.layout.height <= 0f) return; // still resolving
            // Jump past the bottom; ScrollView clamps to the valid range.
            scrollView.scrollOffset = new Vector2(0, container.layout.height);
            container.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        container.RegisterCallback<GeometryChangedEvent>(OnGeometry);

        // If layout is already resolved (rebuilds on an already-shown map),
        // the callback above won't fire — do an immediate attempt too.
        if (container.layout.height > 0f)
            scrollView.scrollOffset = new Vector2(0, container.layout.height);
    }

    private VisualElement CreateNode(int levelNumber, bool isCurrent)
    {
        bool isLocked = !isCurrent;

        var node = new VisualElement();
        node.AddToClassList("level-node");

        if (isCurrent) node.AddToClassList("level-node-current");
        else node.AddToClassList("level-node-locked");

        node.pickingMode = PickingMode.Ignore;

        var numLabel = new Label(levelNumber.ToString());
        numLabel.AddToClassList("level-node-number");
        numLabel.pickingMode = PickingMode.Ignore;
        node.Add(numLabel);


        return node;
    }

    private void AnimateNodesIn()
    {
        // Staggered pop-in, but bottom (current level) animates first so it's instantly visible
        int count = nodeElements.Count;
        for (int i = 0; i < count; i++)
        {
            var capturedNode = nodeElements[i];
            // Reverse order: bottom-most node (current) starts at 0 delay
            float delay = (count - 1 - i) * 0.02f;

            capturedNode.transform.scale = Vector3.zero;
            Tween.Delay(delay, () =>
            {
                Tween.Custom(0f, 1f, 0.2f, val =>
                    capturedNode.transform.scale = new Vector3(val, val, 1f),
                    ease: Ease.OutBack);
            });
        }
    }

    // ================================================================
    //  INTERACTION
    // ================================================================

    private void OnPlayClicked()
    {
        if (sceneLoader == null || sceneLoader.IsLoading) return;
        HideMapUI();
        sceneLoader.LoadGameplay();
    }

    private void HideMapUI()
    {
        if (root != null)
            root.AddToClassList("hidden");
    }

    /// <summary>
    /// Called when returning from gameplay. Refreshes the map.
    /// </summary>
    public void Refresh()
    {
        // Re-read progression from PlayerPrefs — gameplay scene may have advanced it
        progression?.ReloadFromDisk();

        var doc = GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
            ForceStretchTemplateContainer(doc.rootVisualElement);

        if (root != null)
            root.RemoveFromClassList("hidden");

        BuildMap();
    }
}
