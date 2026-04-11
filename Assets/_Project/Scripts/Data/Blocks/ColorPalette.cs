using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A palette of block colors used by a level (or set of levels). Acts as a
/// lookup from string ids in JSON to <see cref="BlockColor"/> assets so no
/// enum explosion is needed when designers add new colors.
/// </summary>
[CreateAssetMenu(menuName = "BlockFlow/Data/Color Palette", fileName = "ColorPalette_")]
public sealed class ColorPalette : ScriptableObject
{
    [SerializeField, Tooltip("All colors available to levels that use this palette.")]
    private BlockColor[] colors;

    public IReadOnlyList<BlockColor> Colors => colors;

    /// <summary>
    /// Linear-scan lookup by color id. Palettes are tiny (&lt; 10 entries),
    /// so a hash table would cost more than it saves.
    /// </summary>
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
