public readonly struct BlockDragStartedEvent
{
    public readonly BlockId BlockId;
    public BlockDragStartedEvent(BlockId id) { BlockId = id; }
}

public readonly struct BlockDragEndedEvent
{
    public readonly BlockId BlockId;
    public BlockDragEndedEvent(BlockId id) { BlockId = id; }
}

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

// Signal, not termination: the drag session stays alive after a bump.
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
