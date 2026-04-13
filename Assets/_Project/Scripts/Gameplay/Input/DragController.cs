using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Free-drag controller. The view follows the finger smoothly during a drag,
/// clamped to the maximum reachable distance computed at drag start.
/// On release, the model snaps to the nearest valid cell and the view
/// tweens to match.
/// </summary>
public sealed class DragController : ITickable, IStartable, IDisposable
{
    private readonly IInputService input;
    private readonly LevelContext context;
    private readonly CellSpace cellSpace;
    private readonly BlockViewRegistry viewRegistry;
    private readonly IMovementStrategy movementStrategy;
    private readonly GameplayCameraFitter cameraFitter;
    private readonly IEventBus bus;
    private readonly GameStateService state;

    private readonly List<IDisposable> subs = new List<IDisposable>();

    private const float AxisLockThreshold = 0.25f;
    private const float SnapDuration = 0.12f;

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
        public Vector3 StartViewLocalPos;
        // Max distance the block can slide from start, in local units
        public float MaxPositive;
        public float MaxNegative;
        public bool LimitsComputed;
    }

    private Session session;
    private Tween snapTween;

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

    public void Start()
    {
        subs.Add(bus.Subscribe<LevelEndedEvent>(_ => ClearSession()));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

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

        BlockId blockId = BlockId.None;
        GridPicker.TryPick(localHit, cellSpace, context.Grid, out _, out blockId);

        if (!blockId.IsValid)
        {
            var worldHit = context.GridRoot != null
                ? context.GridRoot.TransformPoint(localHit) : localHit;
            blockId = PickByRendererBounds(worldHit);
        }

        if (!blockId.IsValid) return;
        if (!context.Grid.TryGetBlock(blockId, out var block)) return;
        if (!viewRegistry.TryGet(blockId, out var view)) return;

        if (snapTween.isAlive) snapTween.Stop();

        session = new Session
        {
            Active = true,
            BlockId = blockId,
            View = view,
            BlockLock = block.AxisLock,
            LockedAxis = AxisFromBlockLock(block.AxisLock),
            StartOrigin = block.Origin,
            StartLocalHit = localHit,
            StartViewLocalPos = view.transform.localPosition,
            LimitsComputed = false
        };

        bus.Publish(new BlockDragStartedEvent(blockId));
    }

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

    // ---------- update (free drag) ----------

    private void UpdateDrag(Vector2 screenPos)
    {
        var camera = cameraFitter != null ? cameraFitter.TargetCamera : null;
        if (camera == null || context.GridRoot == null || context.Grid == null) return;

        if (!GridPicker.TryGetLocalHit(screenPos, camera, context.GridRoot, out var localHit))
            return;

        var deltaLocal = localHit - session.StartLocalHit;

        // Lock axis on first significant movement
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

        // Compute slide limits once after axis is locked
        if (!session.LimitsComputed)
        {
            ComputeSlideLimits(block);
            session.LimitsComputed = true;
        }

        // Get finger delta along locked axis
        float axisDelta = session.LockedAxis == DragAxis.Horizontal
            ? deltaLocal.x : deltaLocal.z;

        // Clamp to computed limits
        axisDelta = Mathf.Clamp(axisDelta, -session.MaxNegative, session.MaxPositive);

        // Move view freely to follow finger
        var viewPos = session.StartViewLocalPos;
        if (session.LockedAxis == DragAxis.Horizontal)
            viewPos.x = session.StartViewLocalPos.x + axisDelta;
        else
            viewPos.z = session.StartViewLocalPos.z + axisDelta;

        session.View.transform.localPosition = viewPos;

        // Update model to nearest valid cell (silently, no view tween)
        UpdateModelToNearest(block, axisDelta);
    }

    /// <summary>
    /// Computes max slide distance in both directions from the start origin.
    /// Called once when the axis locks in.
    /// </summary>
    private void ComputeSlideLimits(BlockModel block)
    {
        var grid = context.Grid;
        GridDirection posDir, negDir;

        if (session.LockedAxis == DragAxis.Horizontal)
        {
            posDir = GridDirection.Right;
            negDir = GridDirection.Left;
        }
        else
        {
            posDir = GridDirection.Up;
            negDir = GridDirection.Down;
        }

        // Slide to max positive, count steps, slide back
        int posSteps = 0;
        if (movementStrategy == null || movementStrategy.CanMove(block, posDir))
        {
            posSteps = grid.SlideUntilBlocked(session.BlockId, posDir, 100, out _);
            if (posSteps > 0)
                grid.SlideUntilBlocked(session.BlockId, negDir, posSteps, out _);
        }

        // Slide to max negative, count steps, slide back
        int negSteps = 0;
        if (movementStrategy == null || movementStrategy.CanMove(block, negDir))
        {
            negSteps = grid.SlideUntilBlocked(session.BlockId, negDir, 100, out _);
            if (negSteps > 0)
                grid.SlideUntilBlocked(session.BlockId, posDir, negSteps, out _);
        }

        session.MaxPositive = posSteps * cellSpace.CellSize;
        session.MaxNegative = negSteps * cellSpace.CellSize;
    }

    /// <summary>
    /// Updates the model to the nearest valid cell based on the current
    /// finger offset. Does not tween the view.
    /// </summary>
    private void UpdateModelToNearest(BlockModel block, float axisDelta)
    {
        int desiredOffset = Mathf.RoundToInt(axisDelta / cellSpace.CellSize);

        int currentOffset;
        if (session.LockedAxis == DragAxis.Horizontal)
            currentOffset = block.Origin.x - session.StartOrigin.x;
        else
            currentOffset = block.Origin.y - session.StartOrigin.y;

        int diff = desiredOffset - currentOffset;
        if (diff == 0) return;

        GridDirection direction;
        if (session.LockedAxis == DragAxis.Horizontal)
            direction = diff > 0 ? GridDirection.Right : GridDirection.Left;
        else
            direction = diff > 0 ? GridDirection.Up : GridDirection.Down;

        if (movementStrategy != null && !movementStrategy.CanMove(block, direction))
            return;

        int moved = context.Grid.SlideUntilBlocked(
            session.BlockId, direction, Mathf.Abs(diff), out _);

        if (moved > 0)
            bus.Publish(new BlockSteppedEvent(session.BlockId, moved));
    }

    // ---------- end (snap to nearest cell) ----------

    private void EndDrag()
    {
        var ended = session.BlockId;
        var view = session.View;

        if (snapTween.isAlive) snapTween.Stop();

        if (view != null && view.Model != null)
        {
            var target = cellSpace.ToWorld(view.Model.Origin);
            snapTween = Tween.LocalPosition(
                target: view.transform,
                endValue: target,
                duration: SnapDuration,
                ease: Ease.OutQuad);
        }

        ClearSession();
        bus.Publish(new BlockDragEndedEvent(ended));
    }

    // ---------- helpers ----------

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
