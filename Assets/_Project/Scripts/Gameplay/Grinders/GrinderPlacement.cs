using UnityEngine;

public static class GrinderPlacement
{
    // The grinder mesh has an asymmetric depth profile around its pivot:
    //   Identity root (Left/Right): mesh X spans [-0.33, +0.03] from pivot.
    //   90°Y root     (Top/Bottom): mesh Z spans [-0.03, +0.33] from pivot.
    // These constants place the opening flush with the grid boundary at depthOffset=0.
    private const float FaceDistThick = 0.33f;
    private const float FaceDistThin  = 0.03f;

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

        // Pivot at the low edge of the first covered cell; mesh extends from
        // pivot in +Z (identity) or +X (90°Y).
        float along = position * cs - half;

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
