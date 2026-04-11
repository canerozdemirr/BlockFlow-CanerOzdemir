/// <summary>
/// Default <see cref="IBlockModelFactory"/>. Assigns ids from a simple
/// monotonically increasing counter starting at 1 (so <see cref="BlockId.None"/>
/// stays the zero sentinel) and bakes rotation into the offsets once at spawn
/// time, so downstream code only ever sees already-rotated offsets.
/// </summary>
public sealed class BlockModelFactory : IBlockModelFactory
{
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
