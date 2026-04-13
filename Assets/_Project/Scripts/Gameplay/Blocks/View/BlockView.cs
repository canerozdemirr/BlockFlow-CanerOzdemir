using System;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

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

    // Active dismiss tween, cleaned up in Unbind so a pool release
    // doesn't leak a callback onto a re-acquired instance.
    private Tween dismissTween;

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

    /// <summary>
    /// Clears the model reference, stops any in-flight dismiss tween, and
    /// resets the transform's local scale so the next pool acquire starts
    /// from a known state.
    /// </summary>
    public void Unbind()
    {
        if (dismissTween.isAlive) dismissTween.Stop();
        Model = null;
        transform.localScale = Vector3.one;
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

    /// <summary>
    /// The block slides toward the grinder while a continuous particle effect
    /// at the grinder's center emits small colored cubes that scatter,
    /// simulating the block being ground down.
    /// </summary>
    public void DismissToGrinder(Vector3 slideDir, float slideDist, Vector3 grinderWorldCenter,
        float duration, Action onComplete)
    {
        if (dismissTween.isAlive) dismissTween.Stop();

        Color debrisColor = GetBlockColor();

        Vector3 startPos = transform.localPosition;
        Vector3 startScale = transform.localScale;
        Action callback = onComplete;

        // Spawn continuous particle effect at the grinder's center (raised slightly)
        Transform particles = BlockShatterEffect.SpawnContinuous(
            grinderWorldCenter, debrisColor, slideDir, transform.parent);

        dismissTween = Tween.Custom(0f, 1f, duration, (float t) =>
        {
            // Slide toward the grinder
            transform.localPosition = startPos + slideDir * (t * slideDist);

            // Scale down along the slide axis — block gets "eaten"
            Vector3 scale = startScale;
            float shrink = 1f - t;
            if (slideDir.x != 0) scale.x = startScale.x * Mathf.Max(shrink, 0.01f);
            else if (slideDir.z != 0) scale.z = startScale.z * Mathf.Max(shrink, 0.01f);
            else scale.y = startScale.y * Mathf.Max(shrink, 0.01f);
            transform.localScale = scale;

            // Particles stay fixed at the grinder center — no need to move them
        }, ease: Ease.InQuad);

        dismissTween.OnComplete(() =>
        {
            if (particles != null)
            {
                ParticleSystem ps = particles.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(particles.gameObject, 0.6f);
            }

            transform.localScale = Vector3.one;
            callback?.Invoke();
        });
    }

    private Color GetBlockColor()
    {
        if (colorRenderer == null) return Color.white;
        mpb ??= new MaterialPropertyBlock();
        colorRenderer.GetPropertyBlock(mpb);
        Color c = mpb.GetColor(BaseColorId);
        return c == Color.clear ? Color.white : c;
    }

    /// <summary>Returns half the renderer extent along the given direction.</summary>
    private float GetRendererHalfExtent(Vector3 direction)
    {
        if (colorRenderer == null) return 0.5f;
        Bounds bounds = colorRenderer.bounds;
        Vector3 localDir = direction.normalized;
        return Mathf.Abs(Vector3.Dot(bounds.extents, localDir));
    }

    /// <summary>Fallback dismiss (simple scale-down) if no grinder context is available.</summary>
    public void Dismiss(float duration, Action onComplete)
    {
        if (dismissTween.isAlive) dismissTween.Stop();

        dismissTween = Tween.Scale(transform, Vector3.zero, duration, Ease.InBack);
        Action callback = onComplete;
        dismissTween.OnComplete(() => callback?.Invoke());
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
