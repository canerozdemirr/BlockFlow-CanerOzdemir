using UnityEngine;

/// <summary>
/// The visual representation of a single <see cref="BlockModel"/>. Follows the
/// strict "model first" rule of the project: the view owns no gameplay state,
/// never mutates the model, and only mirrors whatever the simulation layer
/// tells it. Keeping behavior out of the view is what lets Phase 2 run unit
/// tests without any MonoBehaviour involvement.
///
/// Coloring goes through a <see cref="MaterialPropertyBlock"/> so every block
/// shares the same <c>M_Block_Base</c> material instance and batches via GPU
/// instancing. This avoids the classic "one material per color" pitfall that
/// explodes SetPass counts on mobile.
/// </summary>
public sealed class BlockView : MonoBehaviour
{
    [SerializeField, Tooltip("Renderer whose _BaseColor is tinted to match the block's color. Usually the block's mesh renderer.")]
    private Renderer colorRenderer;

    [SerializeField, Tooltip("Optional overlay GameObject shown while the block is iced. Turned on in Bind / RefreshIceOverlay when BlockModel.IsIced is true.")]
    private GameObject iceOverlay;

    [SerializeField, Tooltip("Transform used as the pivot for juicy tweens in Phase 7 (squash, wobble, pop). Never the transform being positioned by the factory.")]
    private Transform visualRoot;

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public BlockModel Model { get; private set; }
    public Transform VisualRoot => visualRoot;

    /// <summary>
    /// Binds the view to a model and snaps to its current position and color.
    /// Called by <see cref="BlockViewFactory"/> immediately after acquiring
    /// an instance from the pool.
    /// </summary>
    public void Bind(BlockModel model, Color color, CellSpace space)
    {
        Model = model;
        ApplyColor(color);
        SyncTransform(space);
        RefreshIceOverlay();
    }

    /// <summary>Clears the model reference so the instance can go back to the pool.</summary>
    public void Unbind()
    {
        Model = null;
    }

    /// <summary>
    /// Aligns the transform with the model's current origin. Callers invoke
    /// this after grid mutations; there is intentionally no Update loop so
    /// idle blocks cost nothing.
    /// </summary>
    public void SyncTransform(CellSpace space)
    {
        if (Model == null || space == null) return;
        transform.localPosition = space.ToWorld(Model.Origin);
    }

    /// <summary>
    /// Re-checks <see cref="BlockModel.IsIced"/> and toggles the ice overlay
    /// accordingly. Called on bind and whenever the global grind counter
    /// ticks ice down (Phase 6).
    /// </summary>
    public void RefreshIceOverlay()
    {
        if (iceOverlay != null)
            iceOverlay.SetActive(Model != null && Model.IsIced);
    }

    // ---------- internals ----------

    private void ApplyColor(Color color)
    {
        if (colorRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        colorRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        colorRenderer.SetPropertyBlock(mpb);
    }
}
