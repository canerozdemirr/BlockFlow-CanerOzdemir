using UnityEngine;

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

    // Only position is overridden; authored prefab rotation/scale pass through
    // so designers can bake in mesh-orientation fixes.
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

    // Uses the same placement conventions as GrinderPlacement so walls sit
    // flush against the tile boundary.
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

        // true = covered by a grinder.
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
                if (!GridEdgeExtensions.TryParse(g.Edge, out var edge)) continue;
                bool[] mask = edge switch
                {
                    GridEdge.Top    => topCov,
                    GridEdge.Bottom => bottomCov,
                    GridEdge.Left   => leftCov,
                    GridEdge.Right  => rightCov,
                    _ => null
                };
                if (mask == null) continue;
                for (int c = g.Position; c < g.Position + g.Width && c < mask.Length; c++)
                    mask[c] = true;
            }
        }

        // The wall mesh at identity extends +X in depth and +Z along the edge;
        // rotations per edge rotate it outward against the grid boundary.
        for (int x = 0; x < size.width; x++)
        {
            if (topCov[x]) continue;
            var pos = new Vector3(x * cs + half, 0f, boundaryTop);
            var rot = Quaternion.Euler(0f, 270f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Top ({x})", parent);
        }

        for (int x = 0; x < size.width; x++)
        {
            if (bottomCov[x]) continue;
            var pos = new Vector3(x * cs - half, 0f, boundaryBottom);
            var rot = Quaternion.Euler(0f, 90f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Bottom ({x})", parent);
        }

        for (int y = 0; y < size.height; y++)
        {
            if (leftCov[y]) continue;
            var pos = new Vector3(boundaryLeft, 0f, y * cs + half);
            var rot = Quaternion.Euler(0f, 180f, 0f);
            SpawnEdgeWall(pos, rot, $"EdgeWall_Left ({y})", parent);
        }

        for (int y = 0; y < size.height; y++)
        {
            if (rightCov[y]) continue;
            var pos = new Vector3(boundaryRight, 0f, y * cs - half);
            var rot = Quaternion.identity;
            SpawnEdgeWall(pos, rot, $"EdgeWall_Right ({y})", parent);
        }

        // Corners bridge the gap where two edge wall lines meet. The corner
        // mesh at identity covers -X and -Z from pivot.
        if (cornerWallPrefab != null)
        {
            SpawnCorner(new Vector3(boundaryLeft, 0f, boundaryBottom),
                Quaternion.identity, "EdgeCorner_BL", parent);

            SpawnCorner(new Vector3(boundaryRight, 0f, boundaryBottom),
                Quaternion.Euler(0f, 270f, 0f), "EdgeCorner_BR", parent);

            SpawnCorner(new Vector3(boundaryLeft, 0f, boundaryTop),
                Quaternion.Euler(0f, 90f, 0f), "EdgeCorner_TL", parent);

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

    public void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }
}
