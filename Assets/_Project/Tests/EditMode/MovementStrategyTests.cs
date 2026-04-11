using NUnit.Framework;

/// <summary>
/// EditMode tests for the movement strategies. These are small but important:
/// they lock down the axis-lock semantics so the drag controller can trust
/// the strategy's answer without re-deriving the rule at the call site.
/// </summary>
public sealed class MovementStrategyTests
{
    private static BlockModel NewBlock(BlockAxisLock axisLock) =>
        new BlockModel(
            new BlockId(1),
            colorId: "Red",
            axisLock: axisLock,
            cellOffsets: new[] { new GridCoord(0, 0) },
            origin: new GridCoord(0, 0),
            iceLevel: 0);

    // ---------- free ----------

    [Test]
    public void FreeMovement_AlwaysAllowsEveryDirection()
    {
        var block = NewBlock(BlockAxisLock.None);
        var strategy = FreeMovementStrategy.Instance;

        Assert.IsTrue(strategy.CanMove(block, GridDirection.Up));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Down));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Left));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Right));
    }

    [Test]
    public void FreeMovement_IgnoresAxisLockOnBlock()
    {
        // Free strategy deliberately ignores the block's axis lock: factories
        // choose this strategy only when the block is unrestricted, so the
        // lock value should not influence anything.
        var lockedBlock = NewBlock(BlockAxisLock.Horizontal);
        Assert.IsTrue(FreeMovementStrategy.Instance.CanMove(lockedBlock, GridDirection.Up));
    }

    // ---------- single axis ----------

    [Test]
    public void SingleAxis_None_AllowsEveryDirection()
    {
        var block = NewBlock(BlockAxisLock.None);
        var strategy = SingleAxisMovementStrategy.Instance;

        Assert.IsTrue(strategy.CanMove(block, GridDirection.Up));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Down));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Left));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Right));
    }

    [Test]
    public void SingleAxis_Horizontal_OnlyAllowsLeftRight()
    {
        var block = NewBlock(BlockAxisLock.Horizontal);
        var strategy = SingleAxisMovementStrategy.Instance;

        Assert.IsTrue(strategy.CanMove(block, GridDirection.Left));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Right));
        Assert.IsFalse(strategy.CanMove(block, GridDirection.Up));
        Assert.IsFalse(strategy.CanMove(block, GridDirection.Down));
    }

    [Test]
    public void SingleAxis_Vertical_OnlyAllowsUpDown()
    {
        var block = NewBlock(BlockAxisLock.Vertical);
        var strategy = SingleAxisMovementStrategy.Instance;

        Assert.IsTrue(strategy.CanMove(block, GridDirection.Up));
        Assert.IsTrue(strategy.CanMove(block, GridDirection.Down));
        Assert.IsFalse(strategy.CanMove(block, GridDirection.Left));
        Assert.IsFalse(strategy.CanMove(block, GridDirection.Right));
    }
}
