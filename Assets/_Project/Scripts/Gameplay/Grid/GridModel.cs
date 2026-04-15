using System.Collections.Generic;

// Move operations use Lift -> CanPlace -> Commit so a block never collides
// with its own cells mid-slide. Grinders are intentionally not modeled here;
// consumption-on-exit is a higher-level concern.
public sealed class GridModel
{
    private struct Cell
    {
        public bool IsWall;
        public BlockId Occupant;

        public bool IsBlocked => IsWall || Occupant.IsValid;
    }

    private readonly Cell[,] cells;
    private readonly Dictionary<BlockId, BlockModel> blocks = new Dictionary<BlockId, BlockModel>();

    public GridSize Size { get; }

    public IReadOnlyDictionary<BlockId, BlockModel> Blocks => blocks;

    public GridModel(GridSize size)
    {
        Size = size;
        cells = new Cell[size.width <= 0 ? 0 : size.width,
                         size.height <= 0 ? 0 : size.height];
    }

    public bool IsInside(GridCoord coord) => Size.Contains(coord);

    public bool IsWall(GridCoord coord) =>
        IsInside(coord) && cells[coord.x, coord.y].IsWall;

    public BlockId GetOccupant(GridCoord coord) =>
        IsInside(coord) ? cells[coord.x, coord.y].Occupant : BlockId.None;

    public bool TryGetBlock(BlockId id, out BlockModel block) =>
        blocks.TryGetValue(id, out block);

    public int BlockCount => blocks.Count;

    // Walls and blocks are mutually exclusive.
    public bool SetWall(GridCoord coord, bool isWall)
    {
        if (!IsInside(coord)) return false;
        ref var c = ref cells[coord.x, coord.y];
        if (isWall && c.Occupant.IsValid) return false;
        c.IsWall = isWall;
        return true;
    }

    public bool TryPlace(BlockModel block, GridCoord origin)
    {
        if (block == null) return false;
        if (blocks.ContainsKey(block.Id)) return false;
        if (!CanPlaceInternal(block, origin)) return false;

        block.SetOrigin(origin);
        CommitInternal(block);
        blocks[block.Id] = block;
        return true;
    }

    public bool Remove(BlockId id)
    {
        if (!blocks.TryGetValue(id, out var block)) return false;
        LiftInternal(block);
        blocks.Remove(id);
        return true;
    }

    public bool TryMoveStep(BlockId id, GridDirection direction, out GridCoord newOrigin)
    {
        if (!blocks.TryGetValue(id, out var block))
        {
            newOrigin = default;
            return false;
        }

        LiftInternal(block);
        var candidate = block.Origin + direction.ToDelta();

        if (CanPlaceInternal(block, candidate))
        {
            block.SetOrigin(candidate);
            CommitInternal(block);
            newOrigin = candidate;
            return true;
        }

        CommitInternal(block);
        newOrigin = block.Origin;
        return false;
    }

    public int SlideUntilBlocked(BlockId id, GridDirection direction, int maxSteps, out GridCoord finalOrigin)
    {
        if (!blocks.TryGetValue(id, out var block))
        {
            finalOrigin = default;
            return 0;
        }

        LiftInternal(block);

        var origin = block.Origin;
        var delta = direction.ToDelta();
        int moved = 0;

        while (moved < maxSteps)
        {
            var next = origin + delta;
            if (!CanPlaceInternal(block, next)) break;
            origin = next;
            moved++;
        }

        block.SetOrigin(origin);
        CommitInternal(block);
        finalOrigin = origin;
        return moved;
    }

    // Block is assumed already lifted before these helpers run.
    private bool CanPlaceInternal(BlockModel block, GridCoord origin)
    {
        var offsets = block.CellOffsets;
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = origin + offsets[i];
            if (!IsInside(cell)) return false;
            if (cells[cell.x, cell.y].IsBlocked) return false;
        }
        return true;
    }

    private void LiftInternal(BlockModel block)
    {
        var offsets = block.CellOffsets;
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (IsInside(cell))
                cells[cell.x, cell.y].Occupant = BlockId.None;
        }
    }

    private void CommitInternal(BlockModel block)
    {
        var offsets = block.CellOffsets;
        for (int i = 0; i < offsets.Length; i++)
        {
            var cell = block.Origin + offsets[i];
            if (IsInside(cell))
                cells[cell.x, cell.y].Occupant = block.Id;
        }
    }
}
