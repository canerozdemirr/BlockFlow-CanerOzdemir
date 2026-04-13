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
    private readonly GameObject cornerWallPrefab;
    private readonly CellSpace cellSpace;
    private readonly float grinderDepthOffset;

    public GridBuilder(GameObject groundTilePrefab, GameObject wallPrefab, GameObject cornerWallPrefab, CellSpace cellSpace, float grinderDepthOffset = 0f)
    {
        this.groundTilePrefab = groundTilePrefab;
        this.wallPrefab = wallPrefab;
        this.cornerWallPrefab = cornerWallPrefab;
        this.cellSpace = cellSpace;
        this.grinderDepthOffset = grinderDepthOffset;
    }

    /// <summary>
    /// Stamps ground tiles for every cell in the grid and wall instances for
    /// any cell flagged as a wall in <paramref name="grid"/>. Everything is
    /// parented to <paramref name="parent"/> so the caller can wipe a level
    /// by destroying the parent's children.
    ///
    /// Only the <i>position</i> of each instance is overridden — its authored
    /// local rotation and scale pass through from the prefab. That lets
    /// designers bake the correct mesh orientation (e.g. a 90° fix for a
    /// sideways FBX import) into the prefab once and trust the builder to
    /// preserve it.
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
                    tile.name = $"Tile ({x},{y})";
                }

                if (wallPrefab != null && grid.IsWall(coord))
                {
                    var wall = Object.Instantiate(wallPrefab, parent);
                    wall.transform.localPosition = localPos;
                    wall.name = $"Wall ({x},{y})";
                }
            }
        }
    }

    /// <summary>
    /// Places wall prefabs along every grid edge cell that is NOT covered by
    /// a grinder. Uses the same placement conventions as <see cref="GrinderPlacement"/>
    /// so walls sit flush against the tile boundary.
    /// </summary>
    public void BuildEdgeWalls(GridModel grid, GrinderDto[] grinders, Transform parent)
    {
        if (wallPrefab == null || grid == null || cellSpace == null) return;

        var size = grid.Size;
        float cs = cellSpace.CellSize;
        float half = cs * 0.5f;

        float boundaryTop    = (size.height - 1) * cs + half;
        float boundaryBottom = -half;
        float boundaryRight  = (size.width  - 1) * cs + half;
        float boundaryLeft   = -half;

        // Build coverage masks: true = covered by a grinder.
        bool[] topCov    = new bool[size.width];
        bool[] bottomCov = new bool[size.width];
        bool[] leftCov   = new bool[size.height];
        bool[] rightCov  = new bool[size.height];

        if (grinders != null)
        {
            for (int i = 0; i < grinders.Length; i++)
            {
                var g = grinders[i];
                if (g == null) continue;
                bool[] mask;
                switch (g.Edge)
                {
                    case "Top":    mask = topCov;    break;
                    case "Bottom": mask = bottomCov; break;
                    case "Left":   mask = leftCov;   break;
                    case "Right":  mask = rightCov;  break;
                    default: continue;
                }
                for (int c = g.Position; c < g.Position + g.Width && c < mask.Length; c++)
                    mask[c] = true;
            }
        }

        // The wall mesh (child rotation 270,180,0) extends +X in depth and
        // +Z along the edge from its pivot at identity rotation.
        // Rotations per edge:
        //   Right:  identity       → depth +X (outward), along +Z, pivot at cell - half
        //   Left:   Euler(0,180,0) → depth -X (outward), along -Z, pivot at cell + half
        //   Bottom: Euler(0,90,0)  → depth -Z (outward), along +X, pivot at cell - half
        //   Top:    Euler(0,270,0) → depth +Z (outward), along -X, pivot at cell + half

        // Top edge
        for (int x = 0; x < size.width; x++)
        {
            if (topCov[x]) continue;
            var pos = new Vector3(x * cs + half, 0f, boundaryTop);
            var rot = Quaternion.Euler(0f, 270f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Top ({x})", parent);
        }

        // Bottom edge
        for (int x = 0; x < size.width; x++)
        {
            if (bottomCov[x]) continue;
            var pos = new Vector3(x * cs - half, 0f, boundaryBottom);
            var rot = Quaternion.Euler(0f, 90f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Bottom ({x})", parent);
        }

        // Left edge
        for (int y = 0; y < size.height; y++)
        {
            if (leftCov[y]) continue;
            var pos = new Vector3(boundaryLeft, 0f, y * cs + half);
            var rot = Quaternion.Euler(0f, 180f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Left ({y})", parent);
        }

        // Right edge
        for (int y = 0; y < size.height; y++)
        {
            if (rightCov[y]) continue;
            var pos = new Vector3(boundaryRight, 0f, y * cs - half);
            var rot = Quaternion.identity;
            SpawnEdgeWall(pos, rot, $"EdgeWall_Right ({y})", parent);
        }

        // Corners — bridge the gap where two edge wall lines meet.
        // The corner mesh at identity covers -X and -Z from pivot (0.30 × 0.30).
        // Each corner is rotated so it fills the outward-facing gap.
        if (cornerWallPrefab != null)
        {
            // Bottom-Left
            SpawnCorner(new Vector3(boundaryLeft, 0f, boundaryBottom),
                Quaternion.identity, "EdgeCorner_BL", parent);

            // Bottom-Right
            SpawnCorner(new Vector3(boundaryRight, 0f, boundaryBottom),
                Quaternion.Euler(0f, 270f, 0f), "EdgeCorner_BR", parent);

            // Top-Left
            SpawnCorner(new Vector3(boundaryLeft, 0f, boundaryTop),
                Quaternion.Euler(0f, 90f, 0f), "EdgeCorner_TL", parent);

            // Top-Right
            SpawnCorner(new Vector3(boundaryRight, 0f, boundaryTop),
                Quaternion.Euler(0f, 180f, 0f), "EdgeCorner_TR", parent);
        }
    }

    private void SpawnEdgeWall(Vector3 localPos, Quaternion localRot, string name, Transform parent)
    {
        var wall = Object.Instantiate(wallPrefab, parent);
        wall.transform.localPosition = localPos;
        wall.transform.localRotation = localRot;
        wall.name = name;
    }

    private void SpawnCorner(Vector3 localPos, Quaternion localRot, string name, Transform parent)
    {
        var corner = Object.Instantiate(cornerWallPrefab, parent);
        corner.transform.localPosition = localPos;
        corner.transform.localRotation = localRot;
        corner.name = name;
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
