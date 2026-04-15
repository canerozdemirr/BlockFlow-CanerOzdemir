public sealed class BlockModelFactory : IBlockModelFactory
{
    // Start at 1 so BlockId.None (0) stays a sentinel.
    private int nextId = 1;

    public BlockModel Create(in BlockSpawnRequest request)
    {
        var rotated = GridMath.Rotate(request.CanonicalOffsets, request.RotationQuarterTurns);
        var id = new BlockId(nextId++);
        return new BlockModel(
            id,
            request.ColorId,
            request.AxisLock,
            rotated,
            request.Origin,
            request.IceLevel);
    }

    public void Reset()
    {
        nextId = 1;
    }
}
