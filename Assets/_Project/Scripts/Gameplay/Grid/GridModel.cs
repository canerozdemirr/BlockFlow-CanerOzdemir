using System.Collections.Generic;

/// <summary>
/// Pure simulation of the puzzle board. Owns the authoritative cell grid and
/// the set of blocks currently placed on it. No Unity types, no MonoBehaviour,
/// no visuals — the view layer observes this model through read-only access
/// and re-syncs after each mutation.
///
/// Design notes:
///
/// <list type="bullet">
///   <item>Blocks occupy multi-cell footprints via their <see cref="BlockModel.CellOffsets"/>.</item>
///   <item>Move operations use an internal <c>Lift → CanPlace → Commit</c> flow so a block never collides with its own cells mid-slide.</item>
///   <item>Grinders are intentionally <b>not</b> modeled here. Consumption-on-exit is a higher-level concern; this class only answers "is a cell blocked".</item>
///   <item>Public surface is narrow (TryPlace / Remove / TryMoveStep / SlideUntilBlocked / queries) to keep the class easy to reason about and easy to test.</item>
/// </list>
/// </summary>
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

    /// <summary>All blocks currently placed on the grid, keyed by id.</summary>
    public IReadOnlyDictionary<BlockId, BlockModel> Blocks => blocks;

    public GridModel(GridSize size)
    {
        Size = size;
        cells = new Cell[size.width <= 0 ? 0 : size.width,
                         size.height <= 0 ? 0 : size.height];
    }

    // ---------- queries ----------

    public bool IsInside(GridCoord coord) => Size.Contains(coord);

    public bool IsWall(GridCoord coord) =>
        IsInside(coord) && cells[coord.x, coord.y].IsWall;

    public BlockId GetOccupant(GridCoord coord) =>
        IsInside(coord) ? cells[coord.x, coord.y].Occupant : BlockId.None;

    public bool TryGetBlock(BlockId id, out BlockModel block) =>
        blocks.TryGetValue(id, out block);

    public int BlockCount => blocks.Count;

    // ---------- mutation ----------

    /// <summary>
    /// Toggles a wall flag on a cell. Cannot be set on a cell already occupied
    /// by a block — walls and blocks are mutually exclusive. Returns whether
    /// the operation succeeded.
    /// </summary>
    public bool SetWall(GridCoord coord, bool isWall)
    {
        if (!IsInside(coord)) return false;
        ref var c = ref cells[coord.x, coord.y];
        if (isWall && c.Occupant.IsValid) return false;
        c.IsWall = isWall;
        return true;
    }

    /// <summary>
    /// Places a block at the given origin. The block must not already be on
    /// the grid and the target footprint must be clear. Returns true on
    /// success.
    /// </summary>
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

    /// <summary>
    /// Removes the block with the given id. Frees all cells it occupied.
    /// Returns true if the block was on the grid.
    /// </summary>
    public bool Remove(BlockId id)
    {
        if (!blocks.TryGetValue(id, out var block)) return false;
        LiftInternal(block);
        blocks.Remove(id);
        return true;
    }

    /// <summary>
    /// Attempts to move the block one cell in the given direction. Writes the
    /// block's post-move origin to <paramref name="newOrigin"/>, whether or
    /// not the move succeeded.
    /// </summary>
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

        // Revert: re-commit at original origin.
        CommitInternal(block);
        newOrigin = block.Origin;
        return false;
    }

    /// <summary>
    /// Slides the block in a direction until it hits a wall, another block,
    /// or the edge of the grid, up to <paramref name="maxSteps"/> cells.
    /// Returns the number of cells actually moved; writes the final origin.
    /// Pass <see cref="int.MaxValue"/> for an unrestricted slide.
    /// </summary>
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

    // ---------- internals ----------

    /// <summary>Check if the block (assumed lifted) can occupy <paramref name="origin"/>.</summary>
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

    /// <summary>Clears the block's current cells. Used before trying to move it.</summary>
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

    /// <summary>Stamps the block's current cells with its id.</summary>
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
