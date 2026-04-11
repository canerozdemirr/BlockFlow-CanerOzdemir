/// <summary>
/// Produces <see cref="GrinderView"/> instances for a given
/// <see cref="GrinderModel"/> / <see cref="GrinderDefinition"/> pair.
/// Abstracted for the same reasons as <see cref="IBlockViewFactory"/>: no
/// direct Instantiate calls from higher layers and transparent pooling.
/// </summary>
public interface IGrinderViewFactory
{
    GrinderView Acquire(GrinderModel model, GrinderDefinition definition, GridSize gridSize);
    void Release(GrinderView view);
    void Clear();
}
