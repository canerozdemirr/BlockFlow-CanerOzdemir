using NUnit.Framework;

/// <summary>
/// EditMode tests for <see cref="GridModel"/>. Covers placement, collisions,
/// step moves, slides, multi-cell footprints, and wall interactions. These
/// tests are the safety net that lets us refactor the simulation layer later
/// without regressing the core gameplay rules.
/// </summary>
public sealed class GridModelTests
{
    // ---------- helpers ----------

    private static GridModel NewGrid(int w, int h) => new GridModel(new GridSize(w, h));

    private static BlockModel NewCube(int id, int x = 0, int y = 0) =>
        new BlockModel(
            new BlockId(id),
            colorId: "Red",
            axisLock: BlockAxisLock.None,
            cellOffsets: new[] { new GridCoord(0, 0) },
            origin: new GridCoord(x, y),
            iceLevel: 0);

    /// <summary>L-shape with 4 cells: (0,0), (0,1), (0,2), (1,0).</summary>
    private static BlockModel NewL(int id, int x = 0, int y = 0) =>
        new BlockModel(
            new BlockId(id),
            colorId: "Blue",
            axisLock: BlockAxisLock.None,
            cellOffsets: new[]
            {
                new GridCoord(0, 0),
                new GridCoord(0, 1),
                new GridCoord(0, 2),
                new GridCoord(1, 0)
            },
            origin: new GridCoord(x, y),
            iceLevel: 0);

    // ---------- placement ----------

    [Test]
    public void TryPlace_OnEmptyGrid_Succeeds()
    {
        var grid = NewGrid(4, 4);
        var block = NewCube(1);

        Assert.IsTrue(grid.TryPlace(block, new GridCoord(2, 2)));
        Assert.AreEqual(new GridCoord(2, 2), block.Origin);
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(2, 2)));
        Assert.AreEqual(1, grid.BlockCount);
    }

    [Test]
    public void TryPlace_OutsideGrid_Fails()
    {
        var grid = NewGrid(4, 4);
        var block = NewCube(1);

        Assert.IsFalse(grid.TryPlace(block, new GridCoord(-1, 0)));
        Assert.IsFalse(grid.TryPlace(block, new GridCoord(4, 0)));
        Assert.IsFalse(grid.TryPlace(block, new GridCoord(0, 4)));
        Assert.AreEqual(0, grid.BlockCount);
    }

    [Test]
    public void TryPlace_OnWall_Fails()
    {
        var grid = NewGrid(4, 4);
        grid.SetWall(new GridCoord(1, 1), true);

        Assert.IsFalse(grid.TryPlace(NewCube(1), new GridCoord(1, 1)));
        Assert.AreEqual(0, grid.BlockCount);
    }

    [Test]
    public void TryPlace_OnExistingBlock_Fails()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(1, 1));

        Assert.IsFalse(grid.TryPlace(NewCube(2), new GridCoord(1, 1)));
        Assert.AreEqual(1, grid.BlockCount);
    }

    [Test]
    public void TryPlace_MultiCellBlock_PartiallyOutsideGrid_Fails()
    {
        var grid = NewGrid(4, 4);
        var l = NewL(1);

        // L extends to y+2, so origin y=3 places its top cell at (0,5) — out of bounds.
        Assert.IsFalse(grid.TryPlace(l, new GridCoord(0, 3)));
        Assert.AreEqual(0, grid.BlockCount);
    }

    [Test]
    public void TryPlace_MultiCellBlock_AllCellsStamped()
    {
        var grid = NewGrid(4, 4);
        var l = NewL(1);
        Assert.IsTrue(grid.TryPlace(l, new GridCoord(1, 1)));

        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(1, 1)));
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(1, 2)));
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(1, 3)));
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(2, 1)));
    }

    // ---------- removal ----------

    [Test]
    public void Remove_FreesAllCells()
    {
        var grid = NewGrid(4, 4);
        var l = NewL(1);
        grid.TryPlace(l, new GridCoord(0, 0));

        Assert.IsTrue(grid.Remove(new BlockId(1)));
        Assert.AreEqual(BlockId.None, grid.GetOccupant(new GridCoord(0, 0)));
        Assert.AreEqual(BlockId.None, grid.GetOccupant(new GridCoord(0, 1)));
        Assert.AreEqual(BlockId.None, grid.GetOccupant(new GridCoord(0, 2)));
        Assert.AreEqual(BlockId.None, grid.GetOccupant(new GridCoord(1, 0)));
        Assert.AreEqual(0, grid.BlockCount);
    }

    [Test]
    public void Remove_UnknownBlock_ReturnsFalse()
    {
        var grid = NewGrid(4, 4);
        Assert.IsFalse(grid.Remove(new BlockId(42)));
    }

    // ---------- step moves ----------

    [Test]
    public void TryMoveStep_IntoEmptyCell_Succeeds()
    {
        var grid = NewGrid(4, 4);
        var cube = NewCube(1);
        grid.TryPlace(cube, new GridCoord(1, 1));

        Assert.IsTrue(grid.TryMoveStep(new BlockId(1), GridDirection.Right, out var newOrigin));
        Assert.AreEqual(new GridCoord(2, 1), newOrigin);
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(2, 1)));
        Assert.AreEqual(BlockId.None, grid.GetOccupant(new GridCoord(1, 1)));
    }

    [Test]
    public void TryMoveStep_IntoWall_FailsWithoutMoving()
    {
        var grid = NewGrid(4, 4);
        grid.SetWall(new GridCoord(2, 1), true);
        grid.TryPlace(NewCube(1), new GridCoord(1, 1));

        Assert.IsFalse(grid.TryMoveStep(new BlockId(1), GridDirection.Right, out var newOrigin));
        Assert.AreEqual(new GridCoord(1, 1), newOrigin);
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(1, 1)));
    }

    [Test]
    public void TryMoveStep_IntoAnotherBlock_FailsWithoutMoving()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(1, 1));
        grid.TryPlace(NewCube(2), new GridCoord(2, 1));

        Assert.IsFalse(grid.TryMoveStep(new BlockId(1), GridDirection.Right, out _));
        Assert.AreEqual(new BlockId(1), grid.GetOccupant(new GridCoord(1, 1)));
        Assert.AreEqual(new BlockId(2), grid.GetOccupant(new GridCoord(2, 1)));
    }

    [Test]
    public void TryMoveStep_OffGridEdge_Fails()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(0, 0));

        Assert.IsFalse(grid.TryMoveStep(new BlockId(1), GridDirection.Left, out var newOrigin));
        Assert.AreEqual(new GridCoord(0, 0), newOrigin);
    }

    [Test]
    public void TryMoveStep_UnknownBlock_ReturnsFalse()
    {
        var grid = NewGrid(4, 4);
        Assert.IsFalse(grid.TryMoveStep(new BlockId(99), GridDirection.Up, out var origin));
        Assert.AreEqual(default(GridCoord), origin);
    }

    // ---------- slides ----------

    [Test]
    public void SlideUntilBlocked_NoObstacles_ReachesEdge()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(0, 0));

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, int.MaxValue, out var finalOrigin);
        Assert.AreEqual(3, moved);
        Assert.AreEqual(new GridCoord(3, 0), finalOrigin);
    }

    [Test]
    public void SlideUntilBlocked_StopsAtWall()
    {
        var grid = NewGrid(5, 1);
        grid.SetWall(new GridCoord(3, 0), true);
        grid.TryPlace(NewCube(1), new GridCoord(0, 0));

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, int.MaxValue, out var finalOrigin);
        Assert.AreEqual(2, moved);
        Assert.AreEqual(new GridCoord(2, 0), finalOrigin);
    }

    [Test]
    public void SlideUntilBlocked_StopsAtOtherBlock()
    {
        var grid = NewGrid(5, 1);
        grid.TryPlace(NewCube(1), new GridCoord(0, 0));
        grid.TryPlace(NewCube(2), new GridCoord(4, 0));

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, int.MaxValue, out var finalOrigin);
        Assert.AreEqual(3, moved);
        Assert.AreEqual(new GridCoord(3, 0), finalOrigin);
        Assert.AreEqual(new BlockId(2), grid.GetOccupant(new GridCoord(4, 0)));
    }

    [Test]
    public void SlideUntilBlocked_RespectsMaxSteps()
    {
        var grid = NewGrid(10, 1);
        grid.TryPlace(NewCube(1), new GridCoord(0, 0));

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, 3, out var finalOrigin);
        Assert.AreEqual(3, moved);
        Assert.AreEqual(new GridCoord(3, 0), finalOrigin);
    }

    [Test]
    public void SlideUntilBlocked_AlreadyAgainstWall_ReturnsZero()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(3, 0));

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, int.MaxValue, out var finalOrigin);
        Assert.AreEqual(0, moved);
        Assert.AreEqual(new GridCoord(3, 0), finalOrigin);
    }

    [Test]
    public void SlideUntilBlocked_MultiCellBlock_CollidesOnAnyCell()
    {
        // A 3x1 horizontal line block. Placing it at x=0 occupies (0,0),(1,0),(2,0).
        // Putting a wall at (4,0) means after sliding +1, the block occupies (1,0)..(3,0)
        // which is still clear, but sliding +2 would put its right cell on (4,0) -> blocked.
        var grid = NewGrid(6, 1);
        var line = new BlockModel(
            new BlockId(1), "Green", BlockAxisLock.None,
            new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(2, 0) },
            new GridCoord(0, 0), 0);
        grid.TryPlace(line, new GridCoord(0, 0));
        grid.SetWall(new GridCoord(4, 0), true);

        int moved = grid.SlideUntilBlocked(new BlockId(1), GridDirection.Right, int.MaxValue, out var finalOrigin);
        Assert.AreEqual(1, moved);
        Assert.AreEqual(new GridCoord(1, 0), finalOrigin);
    }

    // ---------- walls ----------

    [Test]
    public void SetWall_OnOccupiedCell_Fails()
    {
        var grid = NewGrid(4, 4);
        grid.TryPlace(NewCube(1), new GridCoord(1, 1));

        Assert.IsFalse(grid.SetWall(new GridCoord(1, 1), true));
        Assert.IsFalse(grid.IsWall(new GridCoord(1, 1)));
    }
}
