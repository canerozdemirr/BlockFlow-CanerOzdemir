using UnityEngine;

/// <summary>
/// The visual representation of a <see cref="GrinderModel"/>. Mirrors the
/// layout of <see cref="BlockView"/>: the view owns no gameplay logic, only a
/// color renderer tinted via a <see cref="MaterialPropertyBlock"/> and a
/// visual root used by Phase 7 juice animations.
///
/// The "color renderer" is expected to point at a sub-mesh of the grinder
/// prefab that designers authored specifically to carry the matching color
/// (e.g. an inner rim or a glowing slot), while the gray body and teeth
/// render with their own static materials.
/// </summary>
public sealed class GrinderView : MonoBehaviour
{
    [SerializeField, Tooltip("Renderer whose _BaseColor is tinted to match the accepted block color (usually a colored rim or slot).")]
    private Renderer colorRenderer;

    [SerializeField, Tooltip("Transform pivot for Phase 7 chew / pulse animations.")]
    private Transform visualRoot;

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>
    /// The prefab's authored local rotation, cached on first instantiation.
    /// The factory multiplies the edge-specific turn by this base so the
    /// designer's FBX orientation fix is never lost.
    /// </summary>
    public Quaternion BaseRotation { get; private set; }

    public GrinderModel Model { get; private set; }
    public Transform VisualRoot => visualRoot;

    private void Awake()
    {
        BaseRotation = transform.localRotation;
    }

    public void Bind(GrinderModel model, Color color)
    {
        Model = model;
        ApplyColor(color);
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
