using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Config/Shatter Feel", fileName = "ShatterFeelConfig")]
public sealed class ShatterFeelConfig : ScriptableObject
{
    [SerializeField, Range(0f, 1f), Tooltip("How much the warm highlight color is blended into the Sparks subsystem.")]
    private float sparkBlendRatio = 0.3f;

    [SerializeField, Tooltip("Warm highlight color the Sparks subsystem blends toward.")]
    private Color sparkHighlightColor = new Color(1f, 0.9f, 0.6f, 1f);

    [SerializeField, Tooltip("World-units applied along -slideDir at spawn. Positive pulls particles back toward the grid (useful when the cone's natural spread leaks inside the grid); negative pushes them further outward past the grinder. The caller passes the grinder's world center as the spawn point.")]
    private float spawnInsetFromGrinder = 0.25f;

    [SerializeField, Range(0f, 90f), Tooltip("Cone half-angle applied to every particle system under the prefab at spawn. Tighter = fewer particles go sideways/backwards. 0 disables the override (prefab authored value kept).")]
    private float coneAngleOverride = 20f;

    public float SparkBlendRatio => sparkBlendRatio;
    public Color SparkHighlightColor => sparkHighlightColor;
    public float SpawnInsetFromGrinder => spawnInsetFromGrinder;
    public float ConeAngleOverride => coneAngleOverride;
}
