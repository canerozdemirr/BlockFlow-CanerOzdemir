using UnityEngine;

/// <summary>
/// Designer-facing wrapper around a level. Bridges two worlds:
///
/// <list type="bullet">
///   <item>Level geometry lives in a hand-editable JSON file referenced as a TextAsset.</item>
///   <item>Designer-tweakable metadata (palette, optional overrides, display name) lives on the SO itself.</item>
/// </list>
///
/// This lets level layouts be authored/version-controlled as plain JSON while
/// still exposing the important knobs in the inspector.
/// </summary>
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
