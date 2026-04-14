using System;
using System.Collections.Generic;
using UnityEngine;
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
    private readonly CellSpace cellSpace;

    private readonly List<GrinderModel> grinders = new List<GrinderModel>();
    private readonly List<IDisposable> subs = new List<IDisposable>();

    private const float ConsumeTweenDuration = 0.5f;

    public IReadOnlyList<GrinderModel> Grinders => grinders;

    public GrinderService(
        IEventBus bus,
        LevelContext context,
        GameStateService state,
        BlockViewRegistry viewRegistry,
        IBlockViewFactory blockViewFactory,
        CellSpace cellSpace)
    {
        this.bus = bus;
        this.context = context;
        this.state = state;
        this.viewRegistry = viewRegistry;
        this.blockViewFactory = blockViewFactory;
        this.cellSpace = cellSpace;
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

        // First pass: ALL cells of the block must fit within the grinder's
        // coverage range along the edge's parallel axis. This prevents a
        // block that is wider/taller than the grinder from being consumed.
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (!IsInsideGrinderCoverage(cell, grinder)) return false;
        }

        // Second pass: at least one cell must actually touch the grinder's edge.
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (IsOnEdge(cell, grinder.Edge, size))
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
            var slideDir = EdgeToDirection(grinder.Edge);

            // Slide distance must be large enough to push the entire block
            // past the clip plane. Compute from block's cell extent along
            // the slide axis + extra margin.
            float blockExtent = ComputeBlockExtent(block, grinder.Edge);
            float slideDist = (blockExtent + 1.5f) * cellSpace.CellSize;

            // Scale duration proportionally so bigger blocks don't rush through
            float duration = ConsumeTweenDuration + blockExtent * 0.1f;

            int blockParallelExtent = ComputeBlockParallelExtent(block, grinder.Edge);
            var blockEntryPoint = ComputeBlockEdgeCenter(block, grinder);

            captured.DismissToGrinder(slideDir, slideDist, blockEntryPoint, duration,
                () => blockViewFactory.Release(captured), blockParallelExtent);
        }

        bus.Publish(new BlockGroundEvent(id, grinder.Id, colorId));
    }

    /// <summary>
    /// Computes the world-space center of the grinder's coverage area,
    /// raised slightly on Y so particles are visible above the grid.
    /// </summary>
    private Vector3 ComputeGrinderWorldCenter(GrinderModel grinder)
    {
        float centerAlongEdge = (grinder.Position + (grinder.Width - 1) * 0.5f) * cellSpace.CellSize;
        return EdgePointToWorld(grinder.Edge, centerAlongEdge);
    }

    /// <summary>
    /// Converts a point on the given grinder edge (specified by its distance
    /// along the edge, in local units) into world space, raised on Y so
    /// particles render above the grid.
    /// </summary>
    private Vector3 EdgePointToWorld(GridEdge edge, float alongEdgeLocal)
    {
        var gridSize = context.Grid.Size;
        float cs = cellSpace.CellSize;
        Vector3 localPos;
        switch (edge)
        {
            case GridEdge.Right:
                localPos = new Vector3((gridSize.width - 0.5f) * cs, 0.3f, alongEdgeLocal);
                break;
            case GridEdge.Left:
                localPos = new Vector3(-0.5f * cs, 0.3f, alongEdgeLocal);
                break;
            case GridEdge.Top:
                localPos = new Vector3(alongEdgeLocal, 0.3f, (gridSize.height - 0.5f) * cs);
                break;
            case GridEdge.Bottom:
                localPos = new Vector3(alongEdgeLocal, 0.3f, -0.5f * cs);
                break;
            default:
                localPos = Vector3.zero;
                break;
        }

        return context.GridRoot != null ? context.GridRoot.TransformPoint(localPos) : localPos;
    }

    /// <summary>
    /// Returns how many cells the block spans along the grinder's parallel axis.
    /// Used to size the grind particles to match the block, not the grinder.
    /// </summary>
    private static int ComputeBlockParallelExtent(BlockModel block, GridEdge edge)
    {
        var offsets = block.CellOffsets;
        if (offsets == null || offsets.Length == 0) return 1;

        int min = int.MaxValue;
        int max = int.MinValue;

        for (int i = 0; i < offsets.Length; i++)
        {
            // Parallel axis: X for Top/Bottom, Y for Left/Right
            int val = (edge == GridEdge.Top || edge == GridEdge.Bottom)
                ? offsets[i].x
                : offsets[i].y;
            if (val < min) min = val;
            if (val > max) max = val;
        }

        return max - min + 1;
    }

    /// <summary>
    /// Computes the world-space point where the block meets the grinder edge,
    /// centered along the block's cells on that edge. Particles spawn here
    /// so they align with the block, not the grinder's full coverage area.
    /// </summary>
    private Vector3 ComputeBlockEdgeCenter(BlockModel block, GrinderModel grinder)
    {
        var offsets = block.CellOffsets;
        var origin = block.Origin;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            int val = (grinder.Edge == GridEdge.Top || grinder.Edge == GridEdge.Bottom)
                ? (origin.x + offsets[i].x)
                : (origin.y + offsets[i].y);
            if (val < min) min = val;
            if (val > max) max = val;
        }

        float centerAlongEdge = (min + max) * 0.5f * cellSpace.CellSize;
        return EdgePointToWorld(grinder.Edge, centerAlongEdge);
    }

    /// <summary>
    /// Returns how many cells the block extends along the axis perpendicular
    /// to the grinder edge. This determines how far the block must slide
    /// to fully pass through the clip plane.
    /// </summary>
    private static float ComputeBlockExtent(BlockModel block, GridEdge edge)
    {
        var offsets = block.CellOffsets;
        if (offsets == null || offsets.Length == 0) return 1f;

        // For left/right edges, measure extent along X axis
        // For top/bottom edges, measure extent along Y axis
        int min = int.MaxValue;
        int max = int.MinValue;

        for (int i = 0; i < offsets.Length; i++)
        {
            int val;
            if (edge == GridEdge.Left || edge == GridEdge.Right)
                val = offsets[i].x;
            else
                val = offsets[i].y;

            if (val < min) min = val;
            if (val > max) max = val;
        }

        return max - min + 1;
    }

    /// <summary>
    /// Returns the world-space direction a block should slide toward
    /// when being consumed by a grinder on the given edge.
    /// </summary>
    private static Vector3 EdgeToDirection(GridEdge edge)
    {
        switch (edge)
        {
            case GridEdge.Top:    return Vector3.forward;   //  +Z
            case GridEdge.Bottom: return Vector3.back;      //  -Z
            case GridEdge.Left:   return Vector3.left;      //  -X
            case GridEdge.Right:  return Vector3.right;     //  +X
            default:              return Vector3.forward;
        }
    }
}
