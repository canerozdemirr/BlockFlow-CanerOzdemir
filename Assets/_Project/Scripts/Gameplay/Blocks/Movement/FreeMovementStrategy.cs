/// <summary>
/// Default movement rule: every cardinal direction is allowed. Used for
/// unrestricted blocks (those with <see cref="BlockAxisLock.None"/>). Stateless
/// and thread-safe so a shared <see cref="Instance"/> is handed out to every
/// block that qualifies, avoiding per-block allocations.
/// </summary>
public sealed class FreeMovementStrategy : IMovementStrategy
{
    public static readonly FreeMovementStrategy Instance = new FreeMovementStrategy();

    private FreeMovementStrategy() { }

    public bool CanMove(BlockModel block, GridDirection direction) => true;
}
