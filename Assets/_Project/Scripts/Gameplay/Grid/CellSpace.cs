using UnityEngine;

// Cell (0, 0) lives at local (0, 0, 0); global offset comes from the grid
// root's parent transform. Grid lies on XZ so Y is free for stacking and VFX.
public sealed class CellSpace
{
    public float CellSize { get; }

    public CellSpace(float cellSize = 1f)
    {
        CellSize = cellSize > 0f ? cellSize : 1f;
    }

    public Vector3 ToWorld(GridCoord coord)
    {
        return new Vector3(coord.x * CellSize, 0f, coord.y * CellSize);
    }

    public GridCoord ToGrid(Vector3 world)
    {
        return new GridCoord(
            Mathf.RoundToInt(world.x / CellSize),
            Mathf.RoundToInt(world.z / CellSize));
    }
}
