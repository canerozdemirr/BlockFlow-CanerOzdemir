using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

// All juice tweens target BlockView.VisualRoot (child transform), not the
// prefab root — the drag controller tweens root position and would fight
// tweens on the same transform's localScale.
public sealed class BlockJuiceService : IStartable, IDisposable
{
    private const float PickupScale    = 1.08f;
    private const float PickupDuration = 0.10f;
    private const float ReleaseDuration = 0.15f;

    private readonly IEventBus bus;
    private readonly IBlockViewRegistry registry;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public BlockJuiceService(IEventBus bus, IBlockViewRegistry registry)
    {
        this.bus = bus;
        this.registry = registry;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockDragStartedEvent>(OnDragStarted));
        subs.Add(bus.Subscribe<BlockDragEndedEvent>(OnDragEnded));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    private void OnDragStarted(BlockDragStartedEvent evt)
    {
        var root = ResolveVisualRoot(evt.BlockId);
        if (root == null) return;
        Tween.Scale(root, Vector3.one * PickupScale, PickupDuration, Ease.OutQuad);
    }

    private void OnDragEnded(BlockDragEndedEvent evt)
    {
        var root = ResolveVisualRoot(evt.BlockId);
        if (root == null) return;
        Tween.Scale(root, Vector3.one, ReleaseDuration, Ease.OutBack);
    }

    private Transform ResolveVisualRoot(BlockId id)
    {
        if (!registry.TryGet(id, out var view) || view == null) return null;
        return view.VisualRoot;
    }
}
