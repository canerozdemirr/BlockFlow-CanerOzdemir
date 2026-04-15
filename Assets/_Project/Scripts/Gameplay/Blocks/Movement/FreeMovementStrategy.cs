public sealed class FreeMovementStrategy : IMovementStrategy
{
    public static readonly FreeMovementStrategy Instance = new FreeMovementStrategy();

    private FreeMovementStrategy() { }

    public bool CanMove(BlockModel block, GridDirection direction) => true;
}
