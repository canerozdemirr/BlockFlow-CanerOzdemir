using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default <see cref="IBlockViewFactory"/>. Keeps one <see cref="PrefabPool{T}"/>
/// per <see cref="BlockDefinition.MeshPrefab"/> so acquiring a view for an
/// already-seen shape is amortized free after the first spawn. Tracks every
/// active view so <see cref="Release"/> returns it to the right pool without
/// the caller having to remember which one it came from.
/// </summary>
public sealed class BlockViewFactory : IBlockViewFactory
{
    private readonly ColorPalette palette;
    private readonly CellSpace cellSpace;
    private readonly Transform parent;

    private readonly Dictionary<GameObject, PrefabPool<BlockView>> poolsByPrefab =
        new Dictionary<GameObject, PrefabPool<BlockView>>();

    private readonly Dictionary<BlockView, PrefabPool<BlockView>> activeViews =
        new Dictionary<BlockView, PrefabPool<BlockView>>();

    public BlockViewFactory(ColorPalette palette, CellSpace cellSpace, Transform parent)
    {
        this.palette = palette;
        this.cellSpace = cellSpace;
        this.parent = parent;
    }

    public BlockView Acquire(BlockModel model, BlockDefinition definition)
    {
        if (model == null || definition == null) return null;

        var prefab = definition.MeshPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[BlockViewFactory] BlockDefinition '{definition.ShapeId}' has no mesh prefab assigned.");
            return null;
        }

        if (!poolsByPrefab.TryGetValue(prefab, out var pool))
        {
            var prefabView = prefab.GetComponent<BlockView>();
            if (prefabView == null)
            {
                Debug.LogError($"[BlockViewFactory] BlockDefinition '{definition.ShapeId}' mesh prefab is missing a BlockView component at its root.");
                return null;
            }
            pool = new PrefabPool<BlockView>(prefabView, parent);
            poolsByPrefab[prefab] = pool;
        }

        var view = pool.Get();
        activeViews[view] = pool;

        var color = ResolveColor(model.ColorId);
        view.Bind(model, color, cellSpace);
        return view;
    }

    public void Release(BlockView view)
    {
        if (view == null) return;
        if (!activeViews.TryGetValue(view, out var pool)) return;

        view.Unbind();
        pool.Release(view);
        activeViews.Remove(view);
    }

    public void Clear()
    {
        // Return active views first so pool capacity bookkeeping stays consistent.
        foreach (var pair in activeViews)
        {
            pair.Key.Unbind();
            pair.Value.Release(pair.Key);
        }
        activeViews.Clear();

        foreach (var pool in poolsByPrefab.Values)
            pool.Clear();
        poolsByPrefab.Clear();
    }

    // ---------- internals ----------

    private Color ResolveColor(string colorId)
    {
        if (palette != null && palette.TryGet(colorId, out var entry) && entry != null)
            return entry.DisplayColor;
        return Color.white;
    }
}
