using System;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

// Coloring goes through MaterialPropertyBlock so every block shares one material
// and batches via GPU instancing.
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

    private IceOverlayController iceController;
    private IceFeelConfig iceFeel;

    public BlockModel Model { get; private set; }
    public Transform VisualRoot => visualRoot;

    private void Awake()
    {
        // Pre-cache so the first grind doesn't pay Shader.Find cost.
        if (clipPlaneShader == null)
            clipPlaneShader = Shader.Find("BlockFlow/BlockClipPlane");
    }

    public void Bind(BlockModel model, Color color, CellSpace space, IceFeelConfig iceFeel)
    {
        Model = model;
        this.iceFeel = iceFeel;
        ApplyColor(color);
        SyncTransform(space);
        RefreshIceOverlay();
        RefreshArrows();
    }

    public void Unbind()
    {
        if (dismissTween.isAlive) dismissTween.Stop();
        RestoreOriginalMaterial();
        HideArrows();
        Model = null;
        transform.localScale = Vector3.one;
    }

    // No Update loop — idle blocks cost nothing. Callers invoke after grid mutations.
    public void SyncTransform(CellSpace space)
    {
        if (Model == null || space == null) return;
        transform.localPosition = space.ToWorld(Model.Origin);
    }

    public void RefreshIceOverlay()
    {
        if (iceOverlay == null) return;
        if (iceController == null) iceController = new IceOverlayController(iceOverlay, iceFeel);
        bool isIced = Model != null && Model.IsIced;
        iceController.Refresh(isIced, Model != null ? Model.IceLevel : 0);
    }

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

    public void HideArrows()
    {
        if (arrowHorizontal != null) arrowHorizontal.SetActive(false);
        if (arrowVertical != null)   arrowVertical.SetActive(false);
    }

    // Clip plane shader discards pixels past the grinder edge — block looks swallowed.
    public void DismissToGrinder(Vector3 slideDir, float slideDist, Vector3 grinderWorldCenter,
        float duration, Action onComplete, int grinderWidth = 1)
    {
        if (dismissTween.isAlive) dismissTween.Stop();

        HideArrows();

        Color debrisColor = GetBlockColor();
        Vector3 startPos = transform.localPosition;
        Action callback = onComplete;

        EnableClipPlane(grinderWorldCenter, slideDir);

        Transform particles = BlockShatterEffect.SpawnContinuous(
            grinderWorldCenter, debrisColor, slideDir, transform.parent, grinderWidth);

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

    private void EnableClipPlane(Vector3 planePosition, Vector3 slideDir)
    {
        if (colorRenderer == null) return;

        if (clipPlaneShader == null)
            clipPlaneShader = Shader.Find("BlockFlow/BlockClipPlane");
        if (clipPlaneShader == null) return;

        originalMaterial = colorRenderer.sharedMaterial;

        clipMaterial = new Material(clipPlaneShader);
        Color blockColor = GetBlockColor();
        clipMaterial.SetColor(BaseColorId, blockColor);
        clipMaterial.SetVector(ClipPlanePosId, planePosition);
        // Normal points AGAINST slide direction — pixels on far side get clipped.
        clipMaterial.SetVector(ClipPlaneNormalId, slideDir.normalized);
        clipMaterial.SetFloat(ClipEnabledId, 1f);

        colorRenderer.material = clipMaterial;
    }

    private void RestoreOriginalMaterial()
    {
        if (colorRenderer != null && originalMaterial != null)
        {
            colorRenderer.sharedMaterial = originalMaterial;
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

    private float GetRendererHalfExtent(Vector3 direction)
    {
        if (colorRenderer == null) return 0.5f;
        Bounds bounds = colorRenderer.bounds;
        Vector3 localDir = direction.normalized;
        return Mathf.Abs(Vector3.Dot(bounds.extents, localDir));
    }

    // Fallback scale-down dismiss when no grinder context is available.
    public void Dismiss(float duration, Action onComplete)
    {
        if (dismissTween.isAlive) dismissTween.Stop();

        dismissTween = Tween.Scale(transform, Vector3.zero, duration, Ease.InBack);
        Action callback = onComplete;
        dismissTween.OnComplete(() => callback?.Invoke());
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
