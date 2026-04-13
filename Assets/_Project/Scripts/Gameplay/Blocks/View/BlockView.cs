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

    [SerializeField, Tooltip("Horizontal arrow indicator quad (child of prefab, starts inactive).")]
    private GameObject arrowHorizontal;

    [SerializeField, Tooltip("Vertical arrow indicator quad (child of prefab, starts inactive).")]
    private GameObject arrowVertical;

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ClipPlanePosId = Shader.PropertyToID("_ClipPlanePos");
    private static readonly int ClipPlaneNormalId = Shader.PropertyToID("_ClipPlaneNormal");
    private static readonly int ClipEnabledId = Shader.PropertyToID("_ClipEnabled");

    private static Shader clipPlaneShader;
    private Material originalMaterial;
    private Material clipMaterial;

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
        RefreshArrows();
    }

    /// <summary>
    /// Clears the model reference, stops any in-flight dismiss tween, and
    /// resets the transform's local scale so the next pool acquire starts
    /// from a known state.
    /// </summary>
    public void Unbind()
    {
        if (dismissTween.isAlive) dismissTween.Stop();
        RestoreOriginalMaterial();
        HideArrows();
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
    /// Re-checks <see cref="BlockModel.IsIced"/> and toggles the ice overlay.
    /// Updates the ice count text and overlay opacity based on ice level.
    /// Called on bind and whenever the global grind counter ticks ice down.
    /// </summary>
    public void RefreshIceOverlay()
    {
        if (iceOverlay == null) return;

        bool isIced = Model != null && Model.IsIced;
        iceOverlay.SetActive(isIced);

        if (!isIced) return;

        // Update ice count text
        var textMesh = iceOverlay.GetComponentInChildren<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = Model.IceLevel.ToString();

            // Create a runtime material from the font's own material, swapping the
            // shader to AlwaysOnTop. This keeps the font atlas in sync when TextMesh
            // regenerates its mesh on text changes.
            var textRenderer = textMesh.GetComponent<MeshRenderer>();
            if (textRenderer != null && textMesh.font != null && textMesh.font.material != null)
            {
                var alwaysOnTopShader = Shader.Find("BlockFlow/AlwaysOnTop");
                if (alwaysOnTopShader != null && (textRenderer.material == null || textRenderer.material.shader != alwaysOnTopShader))
                {
                    var mat = new Material(textMesh.font.material);
                    mat.shader = alwaysOnTopShader;
                    mat.color = Color.white;
                    textRenderer.material = mat;
                }
            }
        }

        // Update overlay opacity based on ice level — thicker ice = more opaque
        var overlayRenderer = iceOverlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            float baseAlpha = 0.4f;
            float alphaPerLevel = 0.15f;
            float alpha = Mathf.Clamp01(baseAlpha + Model.IceLevel * alphaPerLevel);
            var mpbIce = new MaterialPropertyBlock();
            overlayRenderer.GetPropertyBlock(mpbIce);
            mpbIce.SetColor("_Color", new Color(0.7f, 0.85f, 1f, alpha));
            overlayRenderer.SetPropertyBlock(mpbIce);
        }
    }

    /// <summary>
    /// Shows the appropriate arrow indicators based on the block's axis lock.
    /// </summary>
    public void RefreshArrows()
    {
        if (Model == null || Model.AxisLock == BlockAxisLock.None)
        {
            HideArrows();
            return;
        }

        if (arrowHorizontal != null) arrowHorizontal.SetActive(Model.AxisLock == BlockAxisLock.Horizontal);
        if (arrowVertical != null)   arrowVertical.SetActive(Model.AxisLock == BlockAxisLock.Vertical);
    }

    /// <summary>Hides both arrow indicators.</summary>
    public void HideArrows()
    {
        if (arrowHorizontal != null) arrowHorizontal.SetActive(false);
        if (arrowVertical != null)   arrowVertical.SetActive(false);
    }

    /// <summary>
    /// The block slides into the grinder at full size. A clip plane shader
    /// discards pixels past the grinder edge, making it look like the block
    /// is being swallowed. Particles play at the grinder contact point.
    /// </summary>
    public void DismissToGrinder(Vector3 slideDir, float slideDist, Vector3 grinderWorldCenter,
        float duration, Action onComplete, int grinderWidth = 1)
    {
        if (dismissTween.isAlive) dismissTween.Stop();

        HideArrows();

        Color debrisColor = GetBlockColor();
        Vector3 startPos = transform.localPosition;
        Action callback = onComplete;

        // Switch to clip plane shader
        EnableClipPlane(grinderWorldCenter, slideDir);

        // Spawn particles at the grinder
        Transform particles = BlockShatterEffect.SpawnContinuous(
            grinderWorldCenter, debrisColor, slideDir, transform.parent, grinderWidth);

        // Slide the block through the grinder at full size — no scaling
        dismissTween = Tween.Custom(0f, 1f, duration, (float t) =>
        {
            transform.localPosition = startPos + slideDir * (t * slideDist);
        }, ease: Ease.InQuad);

        dismissTween.OnComplete(() =>
        {
            if (particles != null)
            {
                ParticleSystem ps = particles.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(particles.gameObject, 0.6f);
            }

            RestoreOriginalMaterial();
            callback?.Invoke();
        });
    }

    /// <summary>
    /// Swaps the renderer to the clip plane shader and sets the clip plane
    /// at the grinder's world position, facing back toward the block.
    /// </summary>
    private void EnableClipPlane(Vector3 planePosition, Vector3 slideDir)
    {
        if (colorRenderer == null) return;

        // Cache and load shader
        if (clipPlaneShader == null)
            clipPlaneShader = Shader.Find("BlockFlow/BlockClipPlane");
        if (clipPlaneShader == null) return;

        // Save original material for restore
        originalMaterial = colorRenderer.sharedMaterial;

        // Create clip material instance with same base color
        clipMaterial = new Material(clipPlaneShader);
        Color blockColor = GetBlockColor();
        clipMaterial.SetColor(BaseColorId, blockColor);
        clipMaterial.SetVector(ClipPlanePosId, planePosition);
        // Normal points AGAINST the slide direction — pixels on the far side get clipped
        clipMaterial.SetVector(ClipPlaneNormalId, slideDir.normalized);
        clipMaterial.SetFloat(ClipEnabledId, 1f);

        colorRenderer.material = clipMaterial;
    }

    /// <summary>Restores the original shared material after grind completes.</summary>
    private void RestoreOriginalMaterial()
    {
        if (colorRenderer != null && originalMaterial != null)
        {
            colorRenderer.sharedMaterial = originalMaterial;
            // Re-apply color via MPB
            Color c = GetBlockColor();
            if (c != Color.clear) ApplyColor(c);
        }
        if (clipMaterial != null)
        {
            Destroy(clipMaterial);
            clipMaterial = null;
        }
        originalMaterial = null;
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
