/// <summary>
/// Produces <see cref="BlockModel"/> instances from primitive spawn data.
/// Abstracted for DI and for tests that need to substitute deterministic id
/// generation. Implementations are expected to handle id allocation and
/// rotation baking so call sites never have to.
/// </summary>
public interface IBlockModelFactory
{
    BlockModel Create(in BlockSpawnRequest request);

    /// <summary>
    /// Resets internal id generation back to the starting value. Useful
    /// between levels (blocks ids can safely restart) and between tests.
    /// </summary>
    void Reset();
}
