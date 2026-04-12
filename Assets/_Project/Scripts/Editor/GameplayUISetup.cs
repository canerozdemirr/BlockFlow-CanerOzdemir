using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click editor menu that creates the full gameplay UI hierarchy inside
/// the active scene. Run once via <c>BlockFlow → Setup Gameplay UI</c>; the
/// script is idempotent — it deletes an existing "[GameplayUI]" root before
/// rebuilding so you can safely re-run after tweaking the code.
///
/// Creates:
/// <list type="bullet">
///   <item>A Screen Space - Overlay Canvas with a portrait CanvasScaler (1080 × 1920).</item>
///   <item>A timer label anchored top-center, wired into a <see cref="GameplayHudView"/>.</item>
///   <item>A Win panel (disabled) with "Level Complete" text, Restart button, and Next button.</item>
///   <item>A Lose panel (disabled) with "Time's Up" text and Retry button.</item>
///   <item>A <see cref="LevelOutcomePopupView"/> that references both panels and all buttons.</item>
/// </list>
///
/// After running, drag the "[GameplayUI]" root into the LifetimeScope's
/// <c>Auto Inject Game Objects</c> list so VContainer finds the <c>[Inject]</c>
/// methods on the UI components.
/// </summary>
public static class GameplayUISetup
{
    [MenuItem("BlockFlow/Setup Gameplay UI")]
    private static void Setup()
    {
        // Remove previous instance if re-running.
        var existing = GameObject.Find("[GameplayUI]");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        // ---- Canvas ----
        var canvasGo = new GameObject("[GameplayUI]");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Setup Gameplay UI");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ---- HUD root ----
        var hudGo = CreateChild(canvasGo, "HUD");
        StretchFull(hudGo);

        // Timer text — top center
        var timerGo = CreateChild(hudGo, "TimerText");
        var timerRect = timerGo.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.3f, 0.93f);
        timerRect.anchorMax = new Vector2(0.7f, 0.98f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;

        var timerTmp = timerGo.AddComponent<TextMeshProUGUI>();
        timerTmp.text = "0:00";
        timerTmp.fontSize = 64;
        timerTmp.alignment = TextAlignmentOptions.Center;
        timerTmp.color = Color.white;
        timerTmp.enableAutoSizing = false;

        var hudView = hudGo.AddComponent<GameplayHudView>();
        SetPrivateField(hudView, "timerText", timerTmp);

        // ---- Win Panel ----
        var winPanel = CreatePanel(canvasGo, "WinPanel", new Color(0.1f, 0.1f, 0.1f, 0.85f));
        winPanel.SetActive(false);

        CreateLabel(winPanel, "WinTitle", "Level Complete!", 72, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));

        var winRestartBtn = CreateButton(winPanel, "WinRestartButton", "Restart",
            new Vector2(0.15f, 0.35f), new Vector2(0.48f, 0.48f));

        var winNextBtn = CreateButton(winPanel, "WinNextButton", "Next Level",
            new Vector2(0.52f, 0.35f), new Vector2(0.85f, 0.48f));

        // ---- Lose Panel ----
        var losePanel = CreatePanel(canvasGo, "LosePanel", new Color(0.15f, 0.05f, 0.05f, 0.85f));
        losePanel.SetActive(false);

        CreateLabel(losePanel, "LoseTitle", "Time's Up!", 72, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));

        var loseRestartBtn = CreateButton(losePanel, "LoseRestartButton", "Retry",
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.48f));

        // ---- Outcome popup view ----
        var popupView = canvasGo.AddComponent<LevelOutcomePopupView>();
        SetPrivateField(popupView, "winPanel", winPanel);
        SetPrivateField(popupView, "losePanel", losePanel);
        SetPrivateField(popupView, "winRestartButton", winRestartBtn.GetComponent<Button>());
        SetPrivateField(popupView, "winNextButton", winNextBtn.GetComponent<Button>());
        SetPrivateField(popupView, "loseRestartButton", loseRestartBtn.GetComponent<Button>());

        Selection.activeGameObject = canvasGo;
        Debug.Log("[GameplayUISetup] UI hierarchy created. Drag [GameplayUI] into the LifetimeScope's Auto Inject Game Objects list.");
    }

    // ---- helpers ----

    private static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreatePanel(GameObject parent, string name, Color bgColor)
    {
        var go = CreateChild(parent, name);
        StretchFull(go);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;
        return go;
    }

    private static void CreateLabel(GameObject parent, string name, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        go.AddComponent<Button>();

        // Button label as a child TMP
        var labelGo = CreateChild(go, "Label");
        StretchFull(labelGo);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 42;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    /// <summary>
    /// Sets a private serialized field via <see cref="SerializedObject"/>.
    /// This is the correct way to wire inspector references from editor
    /// scripts — direct reflection would skip Unity's serialization and
    /// the values wouldn't persist on save.
    /// </summary>
    private static void SetPrivateField(Component target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[GameplayUISetup] Could not find field '{fieldName}' on {target.GetType().Name}.");
        }
    }
}
