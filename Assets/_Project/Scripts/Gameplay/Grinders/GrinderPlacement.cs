using UnityEngine;

/// <summary>
/// Pure pose math for placing a grinder on an edge of the grid.
///
/// Conventions:
/// <list type="bullet">
///   <item>Cell (0, 0) center is at local origin (0, 0, 0).</item>
///   <item>The along-edge position aligns with cell centers (position 0 = cell 0 center).</item>
///   <item><see cref="depthOffset"/> controls how far out from the grid boundary
///     the grinder sits. 0 = opening flush at boundary, positive = further out.</item>
///   <item>Rotation is an edge-specific delta. The factory multiplies it by the
///     prefab's authored base rotation.</item>
/// </list>
/// </summary>
public static class GrinderPlacement
{
    // The grinder mesh (all widths) has an asymmetric depth profile around
    // its pivot. After the child's authored rotation (270,180,0):
    //
    //   Identity root (Left/Right): mesh X spans [-0.33, +0.03] from pivot.
    //   90°Y root     (Top/Bottom): mesh Z spans [-0.03, +0.33] from pivot.
    //
    // The grid-facing surface sits at different distances from the pivot
    // depending on which edge the grinder is on. These constants let us
    // place the opening flush against the grid boundary when depthOffset = 0.
    private const float FaceDistThick = 0.33f; // Right / Bottom grid-facing face
    private const float FaceDistThin  = 0.03f; // Left  / Top    grid-facing face

    /// <summary>
    /// Computes the world-local pose for a grinder.
    /// </summary>
    /// <param name="depthOffset">Distance from the grid boundary to the grinder
    /// opening. 0 = opening flush with tiles. Positive = gap between tiles and
    /// grinder.</param>
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

        // Along the edge: pivot at the low edge of the first covered cell.
        // The grinder mesh extends from its pivot in +Z (identity) or +X (90°Y).
        float along = position * cs - half;

        // Perpendicular: grid boundary.
        float boundaryTop    = (gridSize.height - 1) * cs + half;
        float boundaryBottom = -half;
        float boundaryRight  = (gridSize.width  - 1) * cs + half;
        float boundaryLeft   = -half;

        switch (edge)
        {
            case GridEdge.Top:
                worldPos = new Vector3(along, 0f, boundaryTop + depthOffset + FaceDistThin);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Bottom:
                worldPos = new Vector3(along, 0f, boundaryBottom - depthOffset - FaceDistThick);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Left:
                worldPos = new Vector3(boundaryLeft - depthOffset - FaceDistThin, 0f, along);
                worldRot = Quaternion.identity;
                break;

            case GridEdge.Right:
                worldPos = new Vector3(boundaryRight + depthOffset + FaceDistThick, 0f, along);
                worldRot = Quaternion.identity;
                break;

            default:
                worldPos = Vector3.zero;
                worldRot = Quaternion.identity;
                break;
        }
    }
}
