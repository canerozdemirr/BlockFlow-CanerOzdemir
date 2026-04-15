using UnityEngine;

public sealed class GrinderView : MonoBehaviour
{
    [SerializeField, Tooltip("Renderer whose _BaseColor is tinted to match the accepted block color (usually a colored rim or slot).")]
    private Renderer colorRenderer;

    [SerializeField, Tooltip("Transform pivot for Phase 7 chew / pulse animations.")]
    private Transform visualRoot;

    [SerializeField, Tooltip("Teeth child transform. Rotated +90° on Y for Bottom/Right edges so teeth align with the grid.")]
    private Transform teethRoot;

    private Quaternion teethBaseRotation;

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // Authored local rotation, cached once. Factory multiplies the edge turn by
    // this so the designer's FBX orientation fix is preserved.
    public Quaternion BaseRotation { get; private set; }

    public GrinderModel Model { get; private set; }
    public Transform VisualRoot => visualRoot;

    private void Awake()
    {
        BaseRotation = transform.localRotation;
        if (teethRoot != null)
            teethBaseRotation = teethRoot.localRotation;
    }

    public void Bind(GrinderModel model, Color color)
    {
        Model = model;
        ApplyTeethRotation(model.Edge);
        ApplyColor(color);
    }

    // Top/Left keep authored teeth rotation; Bottom/Right override Y to 90
    // while preserving authored X/Z so teeth stay aligned with the grid.
    private void ApplyTeethRotation(GridEdge edge)
    {
        if (teethRoot == null) return;
        bool flip = edge == GridEdge.Bottom || edge == GridEdge.Right;
        if (flip)
        {
            var e = teethBaseRotation.eulerAngles;
            teethRoot.localRotation = Quaternion.Euler(e.x, 90f, e.z);
        }
        else
        {
            teethRoot.localRotation = teethBaseRotation;
        }
    }

    public void Unbind()
    {
        Model = null;
    }

    private void ApplyColor(Color color)
    {
        if (colorRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        colorRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        colorRenderer.SetPropertyBlock(mpb);
    }
}
