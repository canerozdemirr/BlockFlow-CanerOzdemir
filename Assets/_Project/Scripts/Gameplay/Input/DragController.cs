using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// The single orchestrator for drag-and-drop on the puzzle board. Reads a
/// pointer each tick, hits the grid via <see cref="GridPicker"/>, and
/// translates horizontal/vertical finger motion into <see cref="GridModel"/>
/// slide operations.
///
/// Key design decisions:
///
/// <list type="bullet">
///   <item>
///     <b>Event bus only.</b> Phase 6 replaced the Phase 4 C# events with
///     bus publishes so the grinder service, audio, and VFX can all react
///     to the same signals without the drag controller knowing they exist.
///   </item>
///   <item>
///     <b>Gated by game phase.</b> The tick short-circuits unless
///     <see cref="GameStateService.Current"/> is <see cref="GamePhase.Playing"/>.
///     When a level is won, lost, or unloading, the drag controller sits
///     idle instead of processing a now-invalid grid.
///   </item>
///   <item>
///     <b>Axis lock per drag.</b> Once a session picks an axis — either from
///     the block's authored <see cref="BlockAxisLock"/> or from the first
///     significant finger delta — the block is constrained to that axis for
///     the rest of the drag. This keeps the controls predictable; no
///     diagonal wobble.
///   </item>
///   <item>
///     <b>Absolute targeting, not incremental.</b> Every move computes the
///     desired cell offset from the drag's <i>start</i> origin, then slides
///     the difference from the block's current origin. Dragging back and
///     forth is therefore idempotent — the block ends up wherever the
///     finger says, within what <see cref="GridModel.SlideUntilBlocked"/>
///     allows.
///   </item>
///   <item>
///     <b>Movement strategy as a filter.</b> The controller asks
///     <see cref="IMovementStrategy.CanMove"/> before every slide so adding
///     new movement rules (ice slide, jump, push-chain) later becomes a
///     one-line dispatch, not a tangle of branches.
///   </item>
///   <item>
///     <b>PrimeTween for view follow.</b> The model snaps instantly when
///     cells change; the view chases via a short tween per step so rapid
///     drags don't look like teleports. Previous tween is stopped before
///     the next starts to avoid accumulation.
///   </item>
/// </list>
///
/// Runs as a VContainer entry point — no MonoBehaviour, no scene placement,
/// fully resolved through DI. <see cref="IStartable"/> lets us subscribe to
/// <see cref="LevelEndedEvent"/> so a dangling drag session can't leak across
/// levels.
/// </summary>
public sealed class DragController : ITickable, IStartable, IDisposable
{
    // ---------- dependencies ----------

    private readonly IInputService input;
    private readonly LevelContext context;
    private readonly CellSpace cellSpace;
    private readonly BlockViewRegistry viewRegistry;
    private readonly IMovementStrategy movementStrategy;
    private readonly GameplayCameraFitter cameraFitter;
    private readonly IEventBus bus;
    private readonly GameStateService state;

    private readonly List<IDisposable> subs = new List<IDisposable>();

    // ---------- tuning ----------

    /// <summary>Local-space distance required before an auto-picked axis locks in.</summary>
    private const float AxisLockThreshold = 0.25f;

    /// <summary>PrimeTween duration used for each step of view follow.</summary>
    private const float ViewFollowDuration = 0.08f;

    // ---------- state ----------

    private enum DragAxis { None, Horizontal, Vertical }

    private struct Session
    {
        public bool Active;
        public BlockId BlockId;
        public BlockView View;
        public BlockAxisLock BlockLock;
        public DragAxis LockedAxis;
        public GridCoord StartOrigin;
        public Vector3 StartLocalHit;
    }

    private Session session;
    private Tween viewFollowTween;

    // ---------- ctor ----------

    public DragController(
        IInputService input,
        LevelContext context,
        CellSpace cellSpace,
        BlockViewRegistry viewRegistry,
        IMovementStrategy movementStrategy,
        GameplayCameraFitter cameraFitter,
        IEventBus bus,
        GameStateService state)
    {
        this.input = input;
        this.context = context;
        this.cellSpace = cellSpace;
        this.viewRegistry = viewRegistry;
        this.movementStrategy = movementStrategy;
        this.cameraFitter = cameraFitter;
        this.bus = bus;
        this.state = state;
    }

    // ---------- lifecycle ----------

    public void Start()
    {
        // If a level ends mid-drag (reload, win, lose), drop the session so
        // the next level starts from a clean state.
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => ClearSession()));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    // ---------- tick ----------

    public void Tick()
    {
        if (state.Current != GamePhase.Playing) return;
        if (!context.IsActive || input == null) return;
        if (!input.TryGetPointer(out var pointer)) return;

        switch (pointer.Phase)
        {
            case PointerPhase.Began:
                BeginDrag(pointer.ScreenPosition);
                break;

            case PointerPhase.Moved:
                if (session.Active) UpdateDrag(pointer.ScreenPosition);
                break;

            case PointerPhase.Ended:
            case PointerPhase.Canceled:
                if (session.Active) EndDrag();
                break;
        }
    }

    // ---------- begin ----------

    private void BeginDrag(Vector2 screenPos)
    {
        var camera = cameraFitter != null ? cameraFitter.TargetCamera : null;
        if (camera == null || context.GridRoot == null || context.Grid == null) return;

        if (!GridPicker.TryGetLocalHit(screenPos, camera, context.GridRoot, out var localHit))
            return;

        // Primary: grid cell occupancy lookup (fast, exact).
        BlockId blockId = BlockId.None;
        GridPicker.TryPick(localHit, cellSpace, context.Grid, out _, out blockId);

        // Fallback: if the cell lookup missed (visual mesh offset from logical
        // grid), check every active block view's renderer bounds against the
        // world-space click point. This lets the player grab any visible part
        // of a block even when the mesh pivot doesn't perfectly match the grid.
        if (!blockId.IsValid)
        {
            var worldHit = context.GridRoot != null
                ? context.GridRoot.TransformPoint(localHit)
                : localHit;
            blockId = PickByRendererBounds(worldHit);
        }

        if (!blockId.IsValid) return;
        if (!context.Grid.TryGetBlock(blockId, out var block)) return;
        if (!viewRegistry.TryGet(blockId, out var view)) return;

        session = new Session
        {
            Active = true,
            BlockId = blockId,
            View = view,
            BlockLock = block.AxisLock,
            LockedAxis = AxisFromBlockLock(block.AxisLock),
            StartOrigin = block.Origin,
            StartLocalHit = localHit
        };

        bus.Publish(new BlockDragStartedEvent(blockId));
    }

    /// <summary>
    /// Iterates every active block view and returns the id of the first one
    /// whose renderer bounds contain the world-space point. O(n) but only
    /// runs on pointer-down when the grid cell lookup misses — not per frame.
    /// </summary>
    private BlockId PickByRendererBounds(Vector3 worldPoint)
    {
        foreach (var pair in context.Grid.Blocks)
        {
            if (!viewRegistry.TryGet(pair.Key, out var view) || view == null) continue;
            var renderer = view.GetComponentInChildren<Renderer>();
            if (renderer == null) continue;
            if (renderer.bounds.Contains(worldPoint))
                return pair.Key;
        }
        return BlockId.None;
    }

    // ---------- update ----------

    private void UpdateDrag(Vector2 screenPos)
    {
        var camera = cameraFitter != null ? cameraFitter.TargetCamera : null;
        if (camera == null || context.GridRoot == null || context.Grid == null) return;

        if (!GridPicker.TryGetLocalHit(screenPos, camera, context.GridRoot, out var localHit))
            return;

        var deltaLocal = localHit - session.StartLocalHit;

        // If no axis is locked yet, wait for a significant finger delta, then pick the dominant one.
        if (session.LockedAxis == DragAxis.None)
        {
            float absX = Mathf.Abs(deltaLocal.x);
            float absZ = Mathf.Abs(deltaLocal.z);
            if (absX < AxisLockThreshold && absZ < AxisLockThreshold) return;
            session.LockedAxis = absX > absZ ? DragAxis.Horizontal : DragAxis.Vertical;
        }

        if (!context.Grid.TryGetBlock(session.BlockId, out var block))
        {
            ClearSession();
            return;
        }

        // Compute the desired cell offset along the locked axis, relative to drag start.
        int desiredOffset;
        int currentOffset;
        if (session.LockedAxis == DragAxis.Horizontal)
        {
            desiredOffset = Mathf.RoundToInt(deltaLocal.x / cellSpace.CellSize);
            currentOffset = block.Origin.x - session.StartOrigin.x;
        }
        else
        {
            desiredOffset = Mathf.RoundToInt(deltaLocal.z / cellSpace.CellSize);
            currentOffset = block.Origin.y - session.StartOrigin.y;
        }

        int diff = desiredOffset - currentOffset;
        if (diff == 0) return;

        // Translate offset sign into a GridDirection the model understands.
        GridDirection direction;
        if (session.LockedAxis == DragAxis.Horizontal)
            direction = diff > 0 ? GridDirection.Right : GridDirection.Left;
        else
            direction = diff > 0 ? GridDirection.Up : GridDirection.Down;

        if (movementStrategy != null && !movementStrategy.CanMove(block, direction))
            return;

        int requested = Mathf.Abs(diff);
        int moved = context.Grid.SlideUntilBlocked(session.BlockId, direction, requested, out _);

        if (moved > 0)
        {
            bus.Publish(new BlockSteppedEvent(session.BlockId, moved));
            TweenViewToModel();
        }
        else
        {
            // Finger wants to keep going but the model refused — that's a wall/neighbor bump.
            bus.Publish(new BlockBumpedWallEvent(session.BlockId, direction));
        }
    }

    // ---------- end ----------

    private void EndDrag()
    {
        var ended = session.BlockId;
        var view = session.View;

        // Snap the view precisely to the model's final cell. Any in-flight follow tween is killed first.
        if (viewFollowTween.isAlive) viewFollowTween.Stop();
        if (view != null && view.Model != null)
        {
            view.SyncTransform(cellSpace);
        }

        ClearSession();
        bus.Publish(new BlockDragEndedEvent(ended));
    }

    // ---------- helpers ----------

    private void TweenViewToModel()
    {
        if (session.View == null || session.View.Model == null) return;

        if (viewFollowTween.isAlive) viewFollowTween.Stop();

        var target = cellSpace.ToWorld(session.View.Model.Origin);
        viewFollowTween = Tween.LocalPosition(
            target: session.View.transform,
            endValue: target,
            duration: ViewFollowDuration,
            ease: Ease.OutQuad);
    }

    private void ClearSession()
    {
        session = default;
    }

    private static DragAxis AxisFromBlockLock(BlockAxisLock axisLock)
    {
        switch (axisLock)
        {
            case BlockAxisLock.Horizontal: return DragAxis.Horizontal;
            case BlockAxisLock.Vertical:   return DragAxis.Vertical;
            default:                       return DragAxis.None;
        }
    }
}
