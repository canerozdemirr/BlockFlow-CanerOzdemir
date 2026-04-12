using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Hooks drag-lifecycle bus events to small scale tweens on the grabbed
/// <see cref="BlockView.VisualRoot"/>. All juice lives on <c>VisualRoot</c>
/// (a child transform authored into the block prefab) so the drag
/// controller — which tweens the prefab <i>root</i>'s position — never
/// fights with the juice tweens on <i>localScale</i> of the child.
///
/// <list type="bullet">
///   <item><b>DragStart</b> → scale the visual root up by 8% (OutQuad, 100ms). "Lifted in the air" feel.</item>
///   <item><b>DragEnd</b> → scale back to 1 with an OutBack overshoot (150ms). "Slammed back down" feel.</item>
/// </list>
///
/// Wall bumps don't tween the block deliberately — that responsibility is
/// owned by <see cref="CameraShaker"/> (screen shake) and
/// <see cref="AudioFeedbackRouter"/> (wall bump SFX). Layering a block
/// squash on top would fight with the pickup pop scale.
/// </summary>
public sealed class BlockJuiceService : IStartable, IDisposable
{
    private const float PickupScale    = 1.08f;
    private const float PickupDuration = 0.10f;
    private const float ReleaseDuration = 0.15f;

    private readonly IEventBus bus;
    private readonly BlockViewRegistry registry;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public BlockJuiceService(IEventBus bus, BlockViewRegistry registry)
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
