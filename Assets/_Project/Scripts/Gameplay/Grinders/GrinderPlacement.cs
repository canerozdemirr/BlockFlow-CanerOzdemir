using UnityEngine;

/// <summary>
/// Pure pose math for placing a grinder on an edge of the grid.
///
/// Conventions:
/// <list type="bullet">
///   <item>Cell (0, 0) center is at local origin (0, 0, 0).</item>
///   <item>The along-edge position aligns with cell centers (position 0 = cell 0 center).</item>
///   <item><see cref="depthOffset"/> controls how far out from the grid boundary
///     the grinder sits. 0 = flush at boundary, positive = further out.
///     Tune this on the LifetimeScope to match your grinder mesh depth.</item>
///   <item>Rotation is an edge-specific delta. The factory multiplies it by the
///     prefab's authored base rotation.</item>
/// </list>
/// </summary>
public static class GrinderPlacement
{
    /// <summary>
    /// Computes the world-local pose for a grinder.
    /// </summary>
    /// <param name="depthOffset">Extra distance from the grid boundary outward.
    /// 0 = grinder pivot right on the boundary. Increase to push grinder meshes
    /// further out so their opening sits flush against the tiles.</param>
    public static void GetPose(
        GridEdge edge,
        int position,
        int width,
        GridSize gridSize,
        CellSpace cellSpace,
        float depthOffset,
        out Vector3 worldPos,
        out Quaternion worldRot)
    {
        if (cellSpace == null)
        {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            return;
        }

        float cs   = cellSpace.CellSize;
        float half = cs * 0.5f;

        // Along the edge: pivot at the first cell of the coverage span.
        // The grinder mesh extends from its pivot for 'width' cells.
        float along = position * cs;

        // Perpendicular: grid boundary + depth offset.
        // Grid boundary = outermost cell center ± half a cell.
        float boundaryTop    = (gridSize.height - 1) * cs + half;
        float boundaryBottom = -half;
        float boundaryRight  = (gridSize.width  - 1) * cs + half;
        float boundaryLeft   = -half;

        // Top/Bottom grinders rotate 90° around Y so the mesh (authored for
        // left/right edges) aligns with the horizontal edges.
        // Left/Right grinders keep identity — the mesh is already oriented
        // correctly for vertical edges as authored.
        switch (edge)
        {
            case GridEdge.Top:
                worldPos = new Vector3(along - half, 0f, boundaryTop + depthOffset);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Bottom:
                worldPos = new Vector3(along - half, 0f, boundaryBottom - depthOffset);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Left:
                worldPos = new Vector3(boundaryLeft - depthOffset, 0f, along - half);
                worldRot = Quaternion.identity;
                break;

            case GridEdge.Right:
                worldPos = new Vector3(boundaryRight + depthOffset, 0f, along - half);
                worldRot = Quaternion.identity;
                break;

            default:
                worldPos = Vector3.zero;
                worldRot = Quaternion.identity;
                break;
        }
    }
}
