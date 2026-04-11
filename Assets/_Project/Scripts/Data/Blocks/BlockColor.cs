using UnityEngine;

/// <summary>
/// A single color entry authored by designers. Levels reference colors by
/// <see cref="ColorId"/>, so the string is the stable contract; the
/// Unity <see cref="Color"/> and optional <see cref="MaterialOverride"/> are
/// purely visual.
/// </summary>
[CreateAssetMenu(menuName = "BlockFlow/Data/Block Color", fileName = "BlockColor_")]
public sealed class BlockColor : ScriptableObject
{
    [SerializeField, Tooltip("Stable id used in level JSON to reference this color.")]
    private string colorId;

    [SerializeField, Tooltip("RGB tint used by default block materials and UI swatches.")]
    private Color displayColor = Color.white;

    [SerializeField, Tooltip("Optional full material override. If null, block views use the default material tinted with displayColor.")]
    private Material materialOverride;

    public string ColorId => colorId;
    public Color DisplayColor => displayColor;
    public Material MaterialOverride => materialOverride;
}
