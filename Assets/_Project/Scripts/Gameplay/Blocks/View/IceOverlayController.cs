using UnityEngine;

public sealed class IceOverlayController
{
    private static readonly int IceColorId = Shader.PropertyToID("_Color");
    private static Shader alwaysOnTopShader;

    private readonly GameObject overlay;
    private readonly IceFeelConfig feel;

    private MaterialPropertyBlock mpb;
    private Material textMaterial;
    private Renderer overlayRenderer;
    private TextMesh textMesh;
    private MeshRenderer textRenderer;
    private bool cached;

    public IceOverlayController(GameObject overlay, IceFeelConfig feel)
    {
        this.overlay = overlay;
        this.feel = feel;
    }

    public void Refresh(bool isIced, int iceLevel)
    {
        if (overlay == null) return;

        overlay.SetActive(isIced);
        if (!isIced) return;

        if (!cached)
        {
            textMesh = overlay.GetComponentInChildren<TextMesh>();
            textRenderer = textMesh != null ? textMesh.GetComponent<MeshRenderer>() : null;
            overlayRenderer = overlay.GetComponent<Renderer>();
            cached = true;
        }

        if (textMesh != null)
        {
            textMesh.text = iceLevel.ToString();

            if (textMaterial == null && textRenderer != null && textMesh.font != null && textMesh.font.material != null)
            {
                if (alwaysOnTopShader == null)
                    alwaysOnTopShader = Shader.Find("BlockFlow/AlwaysOnTop");
                if (alwaysOnTopShader != null)
                {
                    textMaterial = new Material(textMesh.font.material) { shader = alwaysOnTopShader, color = Color.white };
                    textRenderer.material = textMaterial;
                }
            }
        }

        if (overlayRenderer != null)
        {
            float baseA = feel != null ? feel.BaseAlpha : 0.4f;
            float perLevel = feel != null ? feel.AlphaPerLevel : 0.15f;
            Color tint = feel != null ? feel.TintColor : new Color(0.7f, 0.85f, 1f, 1f);
            float alpha = Mathf.Clamp01(baseA + iceLevel * perLevel);
            if (mpb == null) mpb = new MaterialPropertyBlock();
            overlayRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(IceColorId, new Color(tint.r, tint.g, tint.b, alpha));
            overlayRenderer.SetPropertyBlock(mpb);
        }
    }
}
