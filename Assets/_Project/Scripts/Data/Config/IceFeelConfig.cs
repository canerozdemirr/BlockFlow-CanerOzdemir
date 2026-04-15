using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Config/Ice Feel", fileName = "IceFeelConfig")]
public sealed class IceFeelConfig : ScriptableObject
{
    [SerializeField, Range(0f, 1f), Tooltip("Overlay alpha when ice level == 0 (about to reveal).")]
    private float baseAlpha = 0.4f;

    [SerializeField, Range(0f, 1f), Tooltip("Extra alpha added per remaining ice level.")]
    private float alphaPerLevel = 0.15f;

    [SerializeField, Tooltip("RGB tint applied to the overlay. Alpha is computed from base + level * per-level.")]
    private Color tintColor = new Color(0.7f, 0.85f, 1f, 1f);

    public float BaseAlpha => baseAlpha;
    public float AlphaPerLevel => alphaPerLevel;
    public Color TintColor => tintColor;
}
