using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>
/// Runtime owner of the level's <see cref="GrinderModel"/> list and the logic
/// that consumes blocks when they fit an adjacent grinder. Populated by
/// <see cref="LevelBuilder"/> during <c>Build</c>, cleared on <see cref="LevelEndedEvent"/>.
///
/// <para>
/// <b>Consumption rule</b> (set during planning):<br/>
/// A block is consumable by a grinder G if
/// <list type="bullet">
///   <item>the block's color id matches G's color id,</item>
///   <item>the block isn't iced (iced blocks have an unknown color until revealed),</item>
///   <item>the block's cells that lie on G's grid edge are all within G's coverage range,</item>
///   <item>and at least one of the block's cells actually touches that edge.</item>
/// </list>
/// The "width = max acceptable" rule picked earlier is enforced naturally: any
/// cell outside the grinder's coverage trips the containment check and
/// rejects consumption.
/// </para>
///
/// <para>
/// The service listens for <see cref="BlockDragEndedEvent"/> because the only
/// way a block can reach a new cell is by being dragged. Only the dragged
/// block is checked — no block can move as a side-effect of another block's
/// slide in this game.
/// </para>
/// </summary>
public sealed class GrinderService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly GameStateService state;
    private readonly BlockViewRegistry viewRegistry;
    private readonly IBlockViewFactory blockViewFactory;

    private readonly List<GrinderModel> grinders = new List<GrinderModel>();
    private readonly List<IDisposable> subs = new List<IDisposable>();

    private const float ConsumeTweenDuration = 0.2f;

    public IReadOnlyList<GrinderModel> Grinders => grinders;

    public GrinderService(
        IEventBus bus,
        LevelContext context,
        GameStateService state,
        BlockViewRegistry viewRegistry,
        IBlockViewFactory blockViewFactory)
    {
        this.bus = bus;
        this.context = context;
        this.state = state;
        this.viewRegistry = viewRegistry;
        this.blockViewFactory = blockViewFactory;
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

    /// <summary>
    /// Called by <see cref="LevelBuilder"/> during level spawn. Each grinder
    /// is registered once; the list is cleared on <see cref="LevelEndedEvent"/>.
    /// </summary>
    public void Register(GrinderModel grinder)
    {
        if (grinder != null) grinders.Add(grinder);
    }

    // ---------- drag-end handler ----------

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

    // ---------- consumption check ----------

    private static bool IsBlockConsumableBy(BlockModel block, GrinderModel grinder, GridSize size)
    {
        int touchingCells = 0;
        var offsets = block.CellOffsets;

        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (!IsOnEdge(cell, grinder.Edge, size)) continue;

            // Cell is in the grinder's row/column — it must also fit inside the grinder's coverage.
            if (!IsInsideGrinderCoverage(cell, grinder)) return false;
            touchingCells++;
        }

        return touchingCells > 0;
    }

    private static bool IsOnEdge(GridCoord cell, GridEdge edge, GridSize size)
    {
        switch (edge)
        {
            case GridEdge.Top:    return cell.y == size.height - 1;
            case GridEdge.Bottom: return cell.y == 0;
            case GridEdge.Left:   return cell.x == 0;
            case GridEdge.Right:  return cell.x == size.width - 1;
        }
        return false;
    }

    private static bool IsInsideGrinderCoverage(GridCoord cell, GrinderModel grinder)
    {
        int start = grinder.Position;
        int end = grinder.Position + grinder.Width - 1;
        switch (grinder.Edge)
        {
            case GridEdge.Top:
            case GridEdge.Bottom:
                return cell.x >= start && cell.x <= end;
            case GridEdge.Left:
            case GridEdge.Right:
                return cell.y >= start && cell.y <= end;
        }
        return false;
    }

    // ---------- consumption ----------

    private void Consume(BlockModel block, GrinderModel grinder)
    {
        var id = block.Id;
        var colorId = block.ColorId;

        context.Grid.Remove(id);

        if (viewRegistry.TryGet(id, out var view) && view != null)
        {
            viewRegistry.Unregister(id);
            var captured = view;
            captured.Dismiss(ConsumeTweenDuration, () => blockViewFactory.Release(captured));
        }

        bus.Publish(new BlockGroundEvent(id, grinder.Id, colorId));
    }
}
