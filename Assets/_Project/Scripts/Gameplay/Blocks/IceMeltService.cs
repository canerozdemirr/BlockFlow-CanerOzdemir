using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

/// <summary>
/// Implements the "global grind counter" rule picked during planning: every
/// successful grind decrements the ice counter on every iced block. When a
/// block's counter reaches zero, its view's ice overlay is refreshed and a
/// <see cref="BlockRevealedEvent"/> goes out for Phase 7 VFX to hook.
///
/// Listens for <see cref="BlockGroundEvent"/> — the one event that fires on
/// every successful consumption — so there is no ambiguity about when ice
/// ticks.
/// </summary>
public sealed class IceMeltService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly IBlockViewRegistry viewRegistry;
    private readonly IPrefabLoader prefabLoader;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    // Scratch list used to avoid mutating the blocks dictionary while iterating.
    private readonly List<BlockModel> scratch = new List<BlockModel>();

    public IceMeltService(IEventBus bus, LevelContext context, IBlockViewRegistry viewRegistry, IPrefabLoader prefabLoader)
    {
        this.bus = bus;
        this.context = context;
        this.viewRegistry = viewRegistry;
        this.prefabLoader = prefabLoader;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockGroundEvent>(OnBlockGround));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    private void OnBlockGround(BlockGroundEvent _)
    {
        if (context.Grid == null) return;

        // Copy current blocks into a scratch list so we iterate over a stable
        // snapshot even if future hooks mutate the grid indirectly.
        scratch.Clear();
        foreach (var pair in context.Grid.Blocks)
            scratch.Add(pair.Value);

        for (int i = 0; i < scratch.Count; i++)
        {
            var block = scratch[i];
            if (!block.IsIced) continue;

            int prevLevel = block.IceLevel;
            block.TickIce();

            if (viewRegistry.TryGet(block.Id, out var view) && view != null)
            {
                if (prevLevel > 0 && block.IceLevel == 0)
                {
                    // Fully revealed — hide overlay, spawn ice shatter particles
                    view.RefreshIceOverlay();
                    SpawnIceShatter(view.transform.position);
                    bus.Publish(new BlockRevealedEvent(block.Id));
                }
                else if (block.IsIced)
                {
                    // Still iced but level decreased — update text and opacity
                    view.RefreshIceOverlay();
                }
            }
        }

        scratch.Clear();
    }

    /// <summary>
    /// Spawns the IceShatterEffect prefab at the block's position.
    /// The prefab plays on awake and self-destructs via StopAction.Destroy.
    /// </summary>
    private void SpawnIceShatter(Vector3 worldPosition)
    {
        var prefab = prefabLoader?.Load("IceShatterEffect");
        if (prefab == null) return;
        Object.Instantiate(prefab, worldPosition + Vector3.up * 1.0f, Quaternion.identity);
    }
}
