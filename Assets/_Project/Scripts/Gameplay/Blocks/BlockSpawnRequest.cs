/// <summary>
/// Primitive, Unity-free description of a block about to be spawned. The
/// level builder (Phase 5) translates a <see cref="BlockDto"/> +
/// <see cref="BlockDefinition"/> pair into one of these so the factory never
/// has to know about ScriptableObjects. Keeping this a plain struct makes
/// block spawning trivially unit-testable.
/// </summary>
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
