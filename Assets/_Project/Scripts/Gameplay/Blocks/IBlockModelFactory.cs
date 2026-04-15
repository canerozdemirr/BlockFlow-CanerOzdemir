public interface IBlockModelFactory
{
    BlockModel Create(in BlockSpawnRequest request);
    void Reset();
}
