using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Data/Level Config", fileName = "LevelConfig_")]
public sealed class LevelConfig : ScriptableObject
{
    [SerializeField, Tooltip("JSON payload describing this level's grid, blocks, grinders and walls.")]
    private TextAsset levelJson;

    [SerializeField, Tooltip("Palette used to resolve the color ids referenced in the level JSON.")]
    private ColorPalette palette;

    [SerializeField, Tooltip("Override for the JSON-defined time limit. Use 0 or negative to keep the JSON value.")]
    private float timeLimitOverride;

    [SerializeField, Tooltip("Human-readable name shown in the level select UI.")]
    private string displayName;

    public TextAsset LevelJson => levelJson;
    public ColorPalette Palette => palette;
    public float TimeLimitOverride => timeLimitOverride;
    public string DisplayName => displayName;
}
