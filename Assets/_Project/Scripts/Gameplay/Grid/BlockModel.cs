// Mutation is internal so only GridModel can change position/ice state.
public sealed class BlockModel
{
    private readonly GridCoord[] cellOffsets;

    public BlockId Id { get; }
    public string ColorId { get; }
    public BlockAxisLock AxisLock { get; }

    // Cell offsets in their final, already-rotated form. Origin-relative.
    public GridCoord[] CellOffsets => cellOffsets;

    public GridCoord Origin { get; private set; }
    public int IceLevel { get; private set; }
    public bool IsIced => IceLevel > 0;

    public BlockModel(
        BlockId id,
        string colorId,
        BlockAxisLock axisLock,
        GridCoord[] cellOffsets,
        GridCoord origin,
        int iceLevel)
    {
        Id = id;
        ColorId = colorId;
        AxisLock = axisLock;
        this.cellOffsets = cellOffsets;
        Origin = origin;
        IceLevel = iceLevel < 0 ? 0 : iceLevel;
    }

    internal void SetOrigin(GridCoord origin) => Origin = origin;

    // Driven by the global grind counter: every successful grind decrements
    // every iced block once.
    internal void TickIce()
    {
        if (IceLevel > 0) IceLevel--;
    }
}
