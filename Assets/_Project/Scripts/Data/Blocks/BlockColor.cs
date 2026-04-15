using UnityEngine;

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
