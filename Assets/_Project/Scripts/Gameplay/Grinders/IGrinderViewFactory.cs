public interface IGrinderViewFactory
{
    GrinderView Acquire(GrinderModel model, GrinderDefinition definition, GridSize gridSize);
    void Release(GrinderView view);
    void Clear();
}
