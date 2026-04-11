using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A designer-authored collection of every <see cref="BlockDefinition"/> the
/// project supports. Acts as the string-id → definition lookup consumed by the
/// level validator, the level builder, and the block view factory.
///
/// The first lookup lazily builds an index; <see cref="OnValidate"/> invalidates
/// it so in-editor edits don't serve stale results.
/// </summary>
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

    /// <summary>
    /// Returns the shape-id → definition map. Handy for the level validator
    /// which accepts an <see cref="IReadOnlyDictionary{TKey,TValue}"/> directly.
    /// </summary>
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
