using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Data/Block Definition Catalog", fileName = "BlockDefinitionCatalog")]
public sealed class BlockDefinitionCatalog : ScriptableObject
{
    [SerializeField, Tooltip("All block definitions referenced by level JSON.")]
    private BlockDefinition[] definitions;

    private Dictionary<string, BlockDefinition> byShapeId;

    public IReadOnlyList<BlockDefinition> Definitions => definitions;

    public bool TryGet(string shapeId, out BlockDefinition result)
    {
        EnsureIndex();
        if (!string.IsNullOrEmpty(shapeId) && byShapeId.TryGetValue(shapeId, out result))
            return true;
        result = null;
        return false;
    }

    public IReadOnlyDictionary<string, BlockDefinition> AsDictionary()
    {
        EnsureIndex();
        return byShapeId;
    }

    private void EnsureIndex()
    {
        if (byShapeId != null) return;

        int count = definitions == null ? 0 : definitions.Length;
        byShapeId = new Dictionary<string, BlockDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            var def = definitions[i];
            if (def == null || string.IsNullOrEmpty(def.ShapeId)) continue;
            byShapeId[def.ShapeId] = def;
        }
    }

    private void OnValidate() => byShapeId = null;
}
