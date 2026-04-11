/// <summary>
/// Runtime state of a single block on the board. Deliberately plain C#: no
/// MonoBehaviour, no ScriptableObject references, no Unity types beyond the
/// data-layer <see cref="GridCoord"/>. This keeps the simulation layer fully
/// unit-testable and lets the view layer (Phase 3) follow the model, not the
/// other way around.
///
/// Mutation is <c>internal</c> so only collaborators inside the gameplay
/// assembly (chiefly <see cref="GridModel"/>) can change position or ice
/// state. External systems observe via read-only properties.
/// </summary>
public sealed class BlockModel
{
    private readonly GridCoord[] cellOffsets;

    public BlockId Id { get; }
    public string ColorId { get; }
    public BlockAxisLock AxisLock { get; }

    /// <summary>Cell offsets in their final, already-rotated form. Origin-relative.</summary>
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

    /// <summary>
    /// Decrements the ice counter by one, floored at zero. Driven by the
    /// global grind counter (every successful grind decrements every iced
    /// block once, per the planning decision).
    /// </summary>
    internal void TickIce()
    {
        if (IceLevel > 0) IceLevel--;
    }
}
