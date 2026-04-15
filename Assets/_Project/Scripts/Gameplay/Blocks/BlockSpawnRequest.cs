public readonly struct BlockSpawnRequest
{
    public readonly GridCoord[] CanonicalOffsets;
    public readonly GridCoord Origin;
    public readonly int RotationQuarterTurns;
    public readonly string ColorId;
    public readonly BlockAxisLock AxisLock;
    public readonly int IceLevel;

    public BlockSpawnRequest(
        GridCoord[] canonicalOffsets,
        GridCoord origin,
        int rotationQuarterTurns,
        string colorId,
        BlockAxisLock axisLock,
        int iceLevel)
    {
        CanonicalOffsets = canonicalOffsets;
        Origin = origin;
        RotationQuarterTurns = rotationQuarterTurns;
        ColorId = colorId;
        AxisLock = axisLock;
        IceLevel = iceLevel;
    }
}
