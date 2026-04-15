using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Data/Grinder Definition Catalog", fileName = "GrinderDefinitionCatalog")]
public sealed class GrinderDefinitionCatalog : ScriptableObject
{
    [SerializeField, Tooltip("All grinder definitions available to levels.")]
    private GrinderDefinition[] definitions;

    private Dictionary<int, GrinderDefinition> byWidth;

    public IReadOnlyList<GrinderDefinition> Definitions => definitions;

    public bool TryGet(int width, out GrinderDefinition result)
    {
        EnsureIndex();
        return byWidth.TryGetValue(width, out result);
    }

    private void EnsureIndex()
    {
        if (byWidth != null) return;

        int count = definitions == null ? 0 : definitions.Length;
        byWidth = new Dictionary<int, GrinderDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            var def = definitions[i];
            if (def == null) continue;
            byWidth[def.Width] = def;
        }
    }

    private void OnValidate() => byWidth = null;
}
