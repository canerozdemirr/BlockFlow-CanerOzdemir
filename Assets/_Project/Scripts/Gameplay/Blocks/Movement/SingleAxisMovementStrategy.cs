public sealed class SingleAxisMovementStrategy : IMovementStrategy
{
    public static readonly SingleAxisMovementStrategy Instance = new SingleAxisMovementStrategy();

    private SingleAxisMovementStrategy() { }

    public bool CanMove(BlockModel block, GridDirection direction)
    {
        switch (block.AxisLock)
        {
            case BlockAxisLock.Horizontal:
                return direction == GridDirection.Left || direction == GridDirection.Right;
            case BlockAxisLock.Vertical:
                return direction == GridDirection.Up   || direction == GridDirection.Down;
            default:
                return true;
        }
    }
}
