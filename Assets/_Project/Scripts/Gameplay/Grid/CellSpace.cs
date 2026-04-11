using UnityEngine;

/// <summary>
/// Authoritative mapping between logical grid coordinates and world positions.
/// The simulation layer thinks entirely in <see cref="GridCoord"/>; the view
/// layer thinks entirely in <see cref="Vector3"/>; this class is the single
/// point where those two worlds meet.
///
/// Cell (0, 0) lives at world (0, 0, 0) in <b>local</b> space; any global
/// offset is applied via the parent transform of the grid root. The grid lies
/// on the XZ plane so Y is free for stacking, camera angle, and VFX.
/// </summary>
public sealed class CellSpace
{
    public float CellSize { get; }

    public CellSpace(float cellSize = 1f)
    {
        CellSize = cellSize > 0f ? cellSize : 1f;
    }

    /// <summary>Local-space position of the given cell's origin point.</summary>
    public Vector3 ToWorld(GridCoord coord)
    {
        return new Vector3(coord.x * CellSize, 0f, coord.y * CellSize);
    }

    /// <summary>
    /// Converts a world-space point back to the nearest grid cell. Used by
    /// the drag controller (Phase 4) to translate finger deltas into cells.
    /// </summary>
    public GridCoord ToGrid(Vector3 world)
    {
        return new GridCoord(
            Mathf.RoundToInt(world.x / CellSize),
            Mathf.RoundToInt(world.z / CellSize));
    }
}
