using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

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
        if (progression != null && uiReady)
            BuildMap();
    }

    private void InitUI()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var ve = doc.rootVisualElement;
        if (ve == null) return;

        ve.RegisterCallback<GeometryChangedEvent>(OnTemplateContainerGeometryChanged);
        ForceStretchTemplateContainer(ve);

        root       = ve.Q("ui_map_root");
        scrollView = ve.Q<ScrollView>("ui_map_scroll");
        content    = ve.Q("ui_map_content");
        levelLabel = ve.Q<Label>("ui_map_level_label_value");
        playBtn    = ve.Q<Button>("ui_map_btn_play");

        if (playBtn != null)
            playBtn.clicked += OnPlayClicked;

        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // ScrollView's internal drag/wheel handlers register on the scrollView
            // itself at TrickleDown, so pickingMode on children doesn't stop them.
            // Swallow the raw input events before they reach those handlers.
            // Programmatic scrollOffset (ScrollToBottomWhenReady) still works —
            // it doesn't go through the event pipeline.
            EventCallback<PointerDownEvent>   downCb  = evt => evt.StopImmediatePropagation();
            EventCallback<PointerMoveEvent>   moveCb  = evt => evt.StopImmediatePropagation();
            EventCallback<PointerUpEvent>     upCb    = evt => evt.StopImmediatePropagation();
            EventCallback<PointerCancelEvent> cancCb  = evt => evt.StopImmediatePropagation();
            EventCallback<WheelEvent>         wheelCb = evt => evt.StopImmediatePropagation();

            scrollView.RegisterCallback(downCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(moveCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(upCb,    TrickleDown.TrickleDown);
            scrollView.RegisterCallback(cancCb,  TrickleDown.TrickleDown);
            scrollView.RegisterCallback(wheelCb, TrickleDown.TrickleDown);
        }

        uiReady = root != null && content != null;

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
        // Root must not swallow picks — child buttons still register.
        ve.pickingMode = PickingMode.Ignore;
    }

    private void OnTemplateContainerGeometryChanged(GeometryChangedEvent evt)
    {
        var ve = evt.target as VisualElement;
        if (ve == null) return;

        if (ve.resolvedStyle.height <= 0)
        {
            ForceStretchTemplateContainer(ve);
        }
        else
        {
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

    private void BuildMap()
    {
        if (!uiReady || progression == null) return;

        content.Clear();
        nodeElements.Clear();

        int currentLevelNum = progression.LevelNumber; // 1-based
        int nodesToShow = ExtraLockedLevels + 1;

        // Build top-to-bottom: highest future level at top, current at bottom (offset 0).
        for (int offset = nodesToShow - 1; offset >= 0; offset--)
        {
            int levelNum = currentLevelNum + offset;
            bool isCurrent = (offset == 0);

            var node = CreateNode(levelNum, isCurrent);
            content.Add(node);
            nodeElements.Add(node);

            if (offset > 0)
            {
                var pathLine = new VisualElement();
                pathLine.AddToClassList("path-line");
                pathLine.AddToClassList("path-line-inactive");
                pathLine.pickingMode = PickingMode.Ignore;
                content.Add(pathLine);
            }
        }

        if (levelLabel != null)
            levelLabel.text = $"Level {currentLevelNum}";

        ScrollToBottomWhenReady();
        AnimateNodesIn();
    }

    // schedule.Execute fires the same frame — before USS flex math has produced
    // real child heights, so contentContainer.layout.height reads 0 and the scroll
    // snaps to the top. GeometryChangedEvent fires the moment heights are known.
    private void ScrollToBottomWhenReady()
    {
        if (scrollView == null) return;
        var container = scrollView.contentContainer;
        if (container == null) return;

        void OnGeometry(GeometryChangedEvent evt)
        {
            if (container.layout.height <= 0f) return;
            // Jump past the bottom; ScrollView clamps to the valid range.
            scrollView.scrollOffset = new Vector2(0, container.layout.height);
            container.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        container.RegisterCallback<GeometryChangedEvent>(OnGeometry);

        // Rebuild on an already-shown map: layout is already resolved so the
        // callback above won't fire — jump immediately instead.
        if (container.layout.height > 0f)
            scrollView.scrollOffset = new Vector2(0, container.layout.height);
    }

    private VisualElement CreateNode(int levelNumber, bool isCurrent)
    {
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
        // Bottom-most node (current) animates first so it's instantly visible.
        int count = nodeElements.Count;
        for (int i = 0; i < count; i++)
        {
            var capturedNode = nodeElements[i];
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

    public void Refresh()
    {
        // Gameplay scene may have advanced progression; re-read from disk.
        progression?.ReloadFromDisk();

        var doc = GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
            ForceStretchTemplateContainer(doc.rootVisualElement);

        if (root != null)
            root.RemoveFromClassList("hidden");

        BuildMap();
    }
}
