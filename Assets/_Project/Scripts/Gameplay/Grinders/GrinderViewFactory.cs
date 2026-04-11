using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default <see cref="IGrinderViewFactory"/>. Mirrors
/// <see cref="BlockViewFactory"/> in layout: one pool per grinder prefab
/// indexed by <see cref="GrinderDefinition.MeshPrefab"/>, an active-view map
/// for return-to-correct-pool on <see cref="Release"/>, and a color resolver
/// that reads from the injected <see cref="ColorPalette"/>.
///
/// Placement is delegated to <see cref="GrinderPlacement"/> so the factory
/// stays focused on object lifetime and doesn't hard-code pose math.
/// </summary>
public sealed class GrinderViewFactory : IGrinderViewFactory
{
    private readonly ColorPalette palette;
    private readonly CellSpace cellSpace;
    private readonly Transform parent;

    private readonly Dictionary<GameObject, PrefabPool<GrinderView>> poolsByPrefab =
        new Dictionary<GameObject, PrefabPool<GrinderView>>();

    private readonly Dictionary<GrinderView, PrefabPool<GrinderView>> activeViews =
        new Dictionary<GrinderView, PrefabPool<GrinderView>>();

    public GrinderViewFactory(ColorPalette palette, CellSpace cellSpace, Transform parent)
    {
        this.palette = palette;
        this.cellSpace = cellSpace;
        this.parent = parent;
    }

    public GrinderView Acquire(GrinderModel model, GrinderDefinition definition, GridSize gridSize)
    {
        if (model == null || definition == null) return null;

        var prefab = definition.MeshPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[GrinderViewFactory] GrinderDefinition (width={definition.Width}) has no mesh prefab assigned.");
            return null;
        }

        if (!poolsByPrefab.TryGetValue(prefab, out var pool))
        {
            var prefabView = prefab.GetComponent<GrinderView>();
            if (prefabView == null)
            {
                Debug.LogError($"[GrinderViewFactory] GrinderDefinition (width={definition.Width}) mesh prefab is missing a GrinderView component at its root.");
                return null;
            }
            pool = new PrefabPool<GrinderView>(prefabView, parent);
            poolsByPrefab[prefab] = pool;
        }

        var view = pool.Get();
        activeViews[view] = pool;

        GrinderPlacement.GetPose(model.Edge, model.Position, model.Width, gridSize, cellSpace, out var pos, out var rot);
        var t = view.transform;
        t.localPosition = pos;
        t.localRotation = rot;

        view.Bind(model, ResolveColor(model.ColorId));
        return view;
    }

    public void Release(GrinderView view)
    {
        if (view == null) return;
        if (!activeViews.TryGetValue(view, out var pool)) return;

        view.Unbind();
        pool.Release(view);
        activeViews.Remove(view);
    }

    public void Clear()
    {
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

    private Color ResolveColor(string colorId)
    {
        if (palette != null && palette.TryGet(colorId, out var entry) && entry != null)
            return entry.DisplayColor;
        return Color.white;
    }
}
