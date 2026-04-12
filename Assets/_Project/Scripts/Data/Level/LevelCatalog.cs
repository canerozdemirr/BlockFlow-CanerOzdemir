using UnityEngine;

/// <summary>
/// Ordered list of levels the player progresses through. Referenced by the
/// <c>LevelProgressionService</c>; authoring-only data, no runtime state.
///
/// Kept as a thin wrapper around an array so levels can be reordered in the
/// inspector without touching code and so tests / tooling can iterate the
/// full set without reflection.
/// </summary>
[CreateAssetMenu(menuName = "BlockFlow/Data/Level Catalog", fileName = "LevelCatalog")]
public sealed class LevelCatalog : ScriptableObject
{
    [SerializeField, Tooltip("Levels played in sequence, from level 1 to the last.")]
    private LevelConfig[] levels;

    public int Count => levels == null ? 0 : levels.Length;

    public LevelConfig GetAt(int index)
    {
        if (levels == null) return null;
        if (index < 0 || index >= levels.Length) return null;
        return levels[index];
    }

    public int IndexOf(LevelConfig config)
    {
        if (levels == null || config == null) return -1;
        for (int i = 0; i < levels.Length; i++)
            if (levels[i] == config) return i;
        return -1;
    }
}
