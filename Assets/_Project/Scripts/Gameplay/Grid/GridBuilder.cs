using UnityEngine;

/// <summary>
/// Instantiates the static visual layer for a grid — ground tiles and walls —
/// given an already-populated <see cref="GridModel"/>. Ground and walls never
/// move or animate during a round, so they are spawned directly (no pool)
/// and torn down between levels via <see cref="Clear"/>.
///
/// Stateless regarding per-level specifics beyond what it parents: each call
/// to <see cref="Build"/> stamps a fresh hierarchy under the supplied parent,
/// so the level loader can hand it a blank root each time.
/// </summary>
public sealed class GridBuilder
{
    private readonly GameObject groundTilePrefab;
    private readonly GameObject wallPrefab;
    private readonly CellSpace cellSpace;

    public GridBuilder(GameObject groundTilePrefab, GameObject wallPrefab, CellSpace cellSpace)
    {
        this.groundTilePrefab = groundTilePrefab;
        this.wallPrefab = wallPrefab;
        this.cellSpace = cellSpace;
    }

    /// <summary>
    /// Stamps ground tiles for every cell in the grid and wall instances for
    /// any cell flagged as a wall in <paramref name="grid"/>. Everything is
    /// parented to <paramref name="parent"/> so the caller can wipe a level
    /// by destroying the parent's children.
    /// </summary>
    public void Build(GridModel grid, Transform parent)
    {
        if (grid == null || cellSpace == null) return;

        var size = grid.Size;
        for (int y = 0; y < size.height; y++)
        {
            for (int x = 0; x < size.width; x++)
            {
                var coord = new GridCoord(x, y);
                var localPos = cellSpace.ToWorld(coord);

                if (groundTilePrefab != null)
                {
                    var tile = Object.Instantiate(groundTilePrefab, parent);
                    tile.transform.localPosition = localPos;
                    tile.transform.localRotation = Quaternion.identity;
                }

                if (wallPrefab != null && grid.IsWall(coord))
                {
                    var wall = Object.Instantiate(wallPrefab, parent);
                    wall.transform.localPosition = localPos;
                    wall.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }

    /// <summary>
    /// Destroys every child of <paramref name="parent"/>. The build step
    /// never leaks references, so this is the cheapest way to tear a level
    /// down before re-building.
    /// </summary>
    public void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }
}
