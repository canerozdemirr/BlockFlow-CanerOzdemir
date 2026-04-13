using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Applies <see cref="Screen.safeArea"/> padding to a named VisualElement
/// inside the UIDocument's visual tree. Updates dynamically on orientation
/// or safe area changes.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class UIToolkitSafeArea : MonoBehaviour
{
    [SerializeField, Tooltip("Name of the VisualElement to apply safe area padding to.")]
    private string targetElementName = "ui_hud_safe_area";

    private UIDocument doc;
    private VisualElement target;
    private Rect lastSafeArea;

    private void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null) return;

        target = doc.rootVisualElement.Q(targetElementName);
        if (target != null) ApplySafeArea();
    }

    private void Update()
    {
        if (target == null) return;
        if (Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var sa = Screen.safeArea;
        lastSafeArea = sa;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        // Convert screen-space safe area to panel-space padding
        // PanelSettings uses ScaleWithScreenSize so we need to account for the scale factor
        var panel = doc.rootVisualElement.panel;
        if (panel == null) return;

        float pixelLeft   = sa.x;
        float pixelRight  = Screen.width - sa.xMax;
        float pixelTop    = Screen.height - sa.yMax;
        float pixelBottom = sa.y;

        // Convert from screen pixels to panel points
        float scale = Screen.width > 0 ? doc.panelSettings.referenceResolution.x / (float)Screen.width : 1f;
        // Use the larger dimension ratio for Expand mode
        float scaleY = Screen.height > 0 ? doc.panelSettings.referenceResolution.y / (float)Screen.height : 1f;
        float s = Mathf.Min(scale, scaleY);

        target.style.paddingLeft   = pixelLeft   * s;
        target.style.paddingRight  = pixelRight  * s;
        target.style.paddingTop    = pixelTop    * s;
        target.style.paddingBottom = pixelBottom * s;
    }
}
