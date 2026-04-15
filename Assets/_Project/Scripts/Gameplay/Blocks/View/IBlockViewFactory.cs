public interface IBlockViewFactory
{
    BlockView Acquire(BlockModel model, BlockDefinition definition);
    void Release(BlockView view);
    void Clear();
}
