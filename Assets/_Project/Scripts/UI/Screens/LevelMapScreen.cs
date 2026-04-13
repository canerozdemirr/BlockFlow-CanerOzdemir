using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit level map: 3 hex nodes in zigzag, dashed path lines,
/// big Play button. Levels loop endlessly after catalog exhausted.
/// Nodes created programmatically as VisualElements.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class LevelMapScreen : MonoBehaviour
{
    private VisualElement root;
    private VisualElement content;
    private Label levelLabel;
    private Button playBtn;
    private Button closeBtn;

    private LevelProgressionService progression;
    private LevelRunner runner;

    private readonly List<VisualElement> spawnedElements = new List<VisualElement>();

    [Inject]
    public void Construct(LevelProgressionService progression, LevelRunner runner)
    {
        this.progression = progression;
        this.runner = runner;
    }

    private bool uiReady;

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

        root      = ve.Q("ui_map_root");
        content   = ve.Q("ui_map_content");
        levelLabel = ve.Q<Label>("ui_map_bottom_level_value");
        playBtn   = ve.Q<Button>("ui_map_btn_play");
        closeBtn  = ve.Q<Button>("ui_map_btn_close");

        if (playBtn != null)  playBtn.clicked  += OnPlayClicked;
        if (closeBtn != null) closeBtn.clicked += OnCloseClicked;

        uiReady = root != null;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (playBtn != null)  playBtn.clicked  -= OnPlayClicked;
        if (closeBtn != null) closeBtn.clicked -= OnCloseClicked;
    }

    public void ShowMap()
    {
        if (!uiReady) InitUI();
        if (root == null) return;
        root.RemoveFromClassList("hidden");
        BuildMap();
    }

    private void HideImmediate()
    {
        UIToolkitPopupAnimator.HideImmediate(root);
    }

    // ================================================================
    //  MAP BUILDER — 3 hex nodes, zigzag, dashed paths
    // ================================================================

    private void BuildMap()
    {
        ClearSpawned();

        if (progression == null || content == null) return;

        int currentIndex = progression.CurrentIndex;
        int catalogCount = progression.Catalog != null ? progression.Catalog.Count : 5;
        if (catalogCount == 0) return;

        int baseLevelNumber = currentIndex + 1;

        // Content area dimensions (approximate from USS)
        float contentWidth = 800f;
        float contentHeight = 1000f;

        // 3 nodes: current (bottom-center), next (middle), next+1 (top)
        float nodeSpacing = 300f;
        float zigzagX = 140f;

        float[] xPositions = {
            contentWidth * 0.5f - zigzagX,   // bottom-left
            contentWidth * 0.5f + zigzagX,   // middle-right
            contentWidth * 0.5f - zigzagX    // top-left
        };
        float startY = contentHeight - 200f;

        for (int i = 0; i < 3; i++)
        {
            float x = xPositions[i];
            float y = startY - i * nodeSpacing;

            // Dashed path to previous node
            if (i > 0)
            {
                float prevX = xPositions[i - 1];
                float prevY = startY - (i - 1) * nodeSpacing;
                CreateDashedPath(prevX, prevY, x, y, i == 0);
            }

            // Node
            bool isCurrent = (i == 0);
            bool isLocked = (i > 0);
            int levelNum = baseLevelNumber + i;
            int catalogIdx = (currentIndex + i) % catalogCount;

            CreateNode(levelNum, x, y, isCurrent, isLocked);
        }

        if (levelLabel != null)
            levelLabel.text = $"LEVEL {baseLevelNumber}";

        AnimateNodesIn();
    }

    private void CreateNode(int levelNumber, float x, float y,
        bool isCurrent, bool isLocked)
    {
        var node = new VisualElement();
        node.AddToClassList("level-node");
        if (isCurrent) node.AddToClassList("level-node-current");
        else if (isLocked) node.AddToClassList("level-node-locked");
        else node.AddToClassList("level-node-completed");

        node.style.left = x - 70;
        node.style.top = y - 80;
        node.pickingMode = PickingMode.Ignore;

        // Hex shape (rounded rect as approximation)
        var hex = new VisualElement();
        hex.AddToClassList("level-node-hex");
        hex.pickingMode = PickingMode.Ignore;

        // Number label
        var numLabel = new Label(levelNumber.ToString());
        numLabel.AddToClassList("level-node-number");
        numLabel.pickingMode = PickingMode.Ignore;

        hex.Add(numLabel);
        node.Add(hex);
        content.Add(node);
        spawnedElements.Add(node);
    }

    private void CreateDashedPath(float x1, float y1, float x2, float y2, bool active)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = Mathf.Sqrt(dx * dx + dy * dy);
        int dashCount = Mathf.Max(1, (int)(length / 24f));

        for (int d = 0; d < dashCount; d++)
        {
            float t = (float)d / dashCount;
            // Skip every other for dashed effect
            if (d % 2 != 0) continue;

            float px = Mathf.Lerp(x1, x2, t);
            float py = Mathf.Lerp(y1, y2, t);

            var dash = new VisualElement();
            dash.AddToClassList("path-dash");
            if (active) dash.AddToClassList("path-segment-active");
            dash.style.left = px - 4;
            dash.style.top = py - 2;
            dash.pickingMode = PickingMode.Ignore;

            content.Add(dash);
            spawnedElements.Add(dash);
        }
    }

    private void AnimateNodesIn()
    {
        int nodeIdx = 0;
        foreach (var elem in spawnedElements)
        {
            if (!elem.ClassListContains("level-node")) continue;

            var captured = elem;
            float delay = nodeIdx * 0.12f;
            captured.transform.scale = Vector3.zero;

            Tween.Delay(delay, () =>
            {
                Tween.Custom(0f, 1f, 0.35f, val =>
                    captured.transform.scale = new Vector3(val, val, 1f),
                    ease: Ease.OutBack);
            });

            nodeIdx++;
        }
    }

    // ================================================================
    //  INTERACTION
    // ================================================================

    private void OnPlayClicked()
    {
        if (progression == null || runner == null) return;
        var level = progression.Current;
        if (level == null) return;

        runner.Load(level);
        HideImmediate();
    }

    private void OnCloseClicked()
    {
        HideImmediate();
    }

    // ================================================================
    //  CLEANUP
    // ================================================================

    private void ClearSpawned()
    {
        foreach (var elem in spawnedElements)
            elem?.RemoveFromHierarchy();
        spawnedElements.Clear();
    }
}
