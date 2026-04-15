using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

// Consumption rule: a block is consumed by grinder G when its color matches,
// it isn't iced (color unknown), ALL its cells fit within G's coverage range
// along the edge's parallel axis, and at least one cell touches G's edge.
public sealed class GrinderService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly GameStateService state;
    private readonly IBlockViewRegistry viewRegistry;
    private readonly IBlockViewFactory blockViewFactory;
    private readonly CellSpace cellSpace;
    private readonly GrinderFeelConfig feel;

    private readonly List<GrinderModel> grinders = new List<GrinderModel>();
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public IReadOnlyList<GrinderModel> Grinders => grinders;

    public GrinderService(
        IEventBus bus,
        LevelContext context,
        GameStateService state,
        IBlockViewRegistry viewRegistry,
        IBlockViewFactory blockViewFactory,
        CellSpace cellSpace,
        GrinderFeelConfig feel)
    {
        this.bus = bus;
        this.context = context;
        this.state = state;
        this.viewRegistry = viewRegistry;
        this.blockViewFactory = blockViewFactory;
        this.cellSpace = cellSpace;
        this.feel = feel;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockDragEndedEvent>(OnDragEnded));
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => grinders.Clear()));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
        grinders.Clear();
    }

    public void Register(GrinderModel grinder)
    {
        if (grinder != null) grinders.Add(grinder);
    }

    private void OnDragEnded(BlockDragEndedEvent evt)
    {
        if (state.Current != GamePhase.Playing) return;
        if (context.Grid == null) return;
        if (!context.Grid.TryGetBlock(evt.BlockId, out var block)) return;

        // Iced blocks can't be consumed; the grinder doesn't know their real color yet.
        if (block.IsIced) return;

        for (int i = 0; i < grinders.Count; i++)
        {
            var grinder = grinders[i];
            if (grinder.ColorId != block.ColorId) continue;
            if (!IsBlockConsumableBy(block, grinder, context.Grid.Size)) continue;

            Consume(block, grinder);
            return;
        }
    }

    // Used by the drag controller to detect eligibility mid-drag so it can
    // cut input and snap-consume immediately.
    public bool TryFindConsumingGrinder(BlockModel block, out GrinderModel grinder)
    {
        grinder = null;
        if (block == null || block.IsIced) return false;
        if (context.Grid == null) return false;

        var size = context.Grid.Size;
        for (int i = 0; i < grinders.Count; i++)
        {
            var g = grinders[i];
            if (g.ColorId != block.ColorId) continue;
            if (!IsBlockConsumableBy(block, g, size)) continue;
            grinder = g;
            return true;
        }
        return false;
    }

    public void ConsumeNow(BlockId blockId)
    {
        if (context.Grid == null) return;
        if (!context.Grid.TryGetBlock(blockId, out var block)) return;
        if (!TryFindConsumingGrinder(block, out var grinder)) return;
        Consume(block, grinder);
    }

    private static bool IsBlockConsumableBy(BlockModel block, GrinderModel grinder, GridSize size)
    {
        int touchingCells = 0;
        var offsets = block.CellOffsets;

        int start = grinder.Position;
        int end = start + grinder.Width - 1;

        // ALL cells must fit within coverage along the edge's parallel axis —
        // prevents oversized blocks from being consumed.
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            int along = grinder.Edge.AlongAxis(cell);
            if (along < start || along > end) return false;
        }

        // And at least one cell must actually touch the grinder's edge.
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (grinder.Edge.ContainsCell(cell, size)) touchingCells++;
        }

        return touchingCells > 0;
    }

    private void Consume(BlockModel block, GrinderModel grinder)
    {
        var id = block.Id;
        var colorId = block.ColorId;

        context.Grid.Remove(id);

        if (viewRegistry.TryGet(id, out var view) && view != null)
        {
            viewRegistry.Unregister(id);
            var captured = view;
            var slideDir = grinder.Edge.ToSlideDirection();

            // Slide distance must push the entire block past the clip plane.
            float blockExtent = GrinderGeometry.BlockPerpendicularExtent(block, grinder.Edge);
            float slideDist = (blockExtent + feel.SlideMargin) * cellSpace.CellSize;

            // Scale duration so bigger blocks don't rush through.
            float duration = feel.ConsumeTweenDuration + blockExtent * feel.DurationPerCellExtent;

            int blockParallelExtent = GrinderGeometry.BlockParallelExtent(block, grinder.Edge);
            var blockEntryPoint = GrinderGeometry.BlockEdgeCenterWorld(
                block, grinder, context.Grid.Size, cellSpace.CellSize, context.GridRoot);

            captured.DismissToGrinder(slideDir, slideDist, blockEntryPoint, duration,
                () =>
                {
                    blockViewFactory.Release(captured);
                    bus.Publish(new BlockGrindCompletedEvent(id));
                },
                blockParallelExtent);
        }
        else
        {
            // Headless: still signal completion so grind-timeline listeners stay correct.
            bus.Publish(new BlockGrindCompletedEvent(id));
        }

        bus.Publish(new BlockGroundEvent(id, grinder.Id, colorId));
    }

}
