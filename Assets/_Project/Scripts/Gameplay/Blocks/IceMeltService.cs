using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

// Global grind counter: every successful grind decrements ice on every iced block.
public sealed class IceMeltService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly IBlockViewRegistry viewRegistry;
    private readonly IPrefabLoader prefabLoader;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    // Avoids mutating the blocks dictionary while iterating.
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
                    view.RefreshIceOverlay();
                    SpawnIceShatter(view.transform.position);
                    bus.Publish(new BlockRevealedEvent(block.Id));
                }
                else if (block.IsIced)
                {
                    view.RefreshIceOverlay();
                }
            }
        }

        scratch.Clear();
    }

    // Prefab plays on awake and self-destructs via StopAction.Destroy.
    private void SpawnIceShatter(Vector3 worldPosition)
    {
        var prefab = prefabLoader?.Load("IceShatterEffect");
        if (prefab == null) return;
        Object.Instantiate(prefab, worldPosition + Vector3.up * 1.0f, Quaternion.identity);
    }
}
