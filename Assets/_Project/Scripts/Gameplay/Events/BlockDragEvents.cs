/// <summary>
/// Fired by <see cref="DragController"/> on pointer-down that hits a block.
/// </summary>
public readonly struct BlockDragStartedEvent
{
    public readonly BlockId BlockId;
    public BlockDragStartedEvent(BlockId id) { BlockId = id; }
}

/// <summary>
/// Fired by <see cref="DragController"/> on pointer-up. This is the moment
/// <see cref="GrinderService"/> checks whether the just-dragged block should
/// be consumed.
/// </summary>
public readonly struct BlockDragEndedEvent
{
    public readonly BlockId BlockId;
    public BlockDragEndedEvent(BlockId id) { BlockId = id; }
}

/// <summary>
/// Fired whenever the model actually moved one or more cells during a drag.
/// Phase 7 hooks this for scrape SFX and micro-juice.
/// </summary>
public readonly struct BlockSteppedEvent
{
    public readonly BlockId BlockId;
    public readonly int Cells;

    public BlockSteppedEvent(BlockId id, int cells)
    {
        BlockId = id;
        Cells = cells;
    }
}

/// <summary>
/// Fired when the player tried to push a block further but was blocked by
/// a wall, a neighbor, or the edge of the grid. The drag session stays
/// alive; this is a signal, not a termination. Phase 7 hooks this for
/// wall-bump shake and SFX.
/// </summary>
public readonly struct BlockBumpedWallEvent
{
    public readonly BlockId BlockId;
    public readonly GridDirection Direction;

    public BlockBumpedWallEvent(BlockId id, GridDirection direction)
    {
        BlockId = id;
        Direction = direction;
    }
}
