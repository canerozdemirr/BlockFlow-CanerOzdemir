/// <summary>
/// Restricts a block to a single axis of travel, as authored on its
/// <see cref="BlockModel.AxisLock"/>. A horizontally-locked block can only
/// slide Left/Right; a vertically-locked block only Up/Down. Blocks whose
/// authored lock is <see cref="BlockAxisLock.None"/> fall through to
/// "allowed", which keeps the strategy safe to use as a default when the
/// factory is uncertain.
/// </summary>
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
