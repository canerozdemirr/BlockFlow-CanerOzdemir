using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Data/Color Palette", fileName = "ColorPalette_")]
public sealed class ColorPalette : ScriptableObject
{
    [SerializeField, Tooltip("All colors available to levels that use this palette.")]
    private BlockColor[] colors;

    public IReadOnlyList<BlockColor> Colors => colors;

    // Linear scan: palettes are tiny (< 10 entries), a hash table costs more than it saves.
    public bool TryGet(string colorId, out BlockColor result)
    {
        if (!string.IsNullOrEmpty(colorId) && colors != null)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                if (c != null && c.ColorId == colorId)
                {
                    result = c;
                    return true;
                }
            }
        }
        result = null;
        return false;
    }
}
