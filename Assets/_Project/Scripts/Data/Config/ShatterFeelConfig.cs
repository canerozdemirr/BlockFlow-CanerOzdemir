using UnityEngine;

/// <summary>
/// Tuning for the grind-particle tint in <see cref="BlockShatterEffect"/>.
/// Assigned at gameplay bootstrap via the static <c>Config</c> hook.
/// </summary>
[CreateAssetMenu(menuName = "BlockFlow/Config/Shatter Feel", fileName = "ShatterFeelConfig")]
public sealed class ShatterFeelConfig : ScriptableObject
{
    [SerializeField, Range(0f, 1f), Tooltip("How much the warm highlight color is blended into the Sparks subsystem.")]
    private float sparkBlendRatio = 0.3f;

    [SerializeField, Tooltip("Warm highlight color the Sparks subsystem blends toward.")]
    private Color sparkHighlightColor = new Color(1f, 0.9f, 0.6f, 1f);

    public float SparkBlendRatio => sparkBlendRatio;
    public Color SparkHighlightColor => sparkHighlightColor;
}
