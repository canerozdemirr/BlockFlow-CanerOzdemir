/// <summary>
/// Produces <see cref="BlockView"/> instances for a given
/// <see cref="BlockModel"/> / <see cref="BlockDefinition"/> pair. Abstracted
/// so higher layers (level builder, tests) never reach into Unity's
/// <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/> directly
/// and so the implementation can add pooling transparently.
/// </summary>
public interface IBlockViewFactory
{
    BlockView Acquire(BlockModel model, BlockDefinition definition);
    void Release(BlockView view);

    /// <summary>
    /// Returns every active view to its pool and drops the pools themselves.
    /// Used between levels and in teardown.
    /// </summary>
    void Clear();
}
