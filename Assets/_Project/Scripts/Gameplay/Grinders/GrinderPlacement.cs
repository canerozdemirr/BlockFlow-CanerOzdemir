using UnityEngine;

/// <summary>
/// Pure pose math for placing a grinder on an edge of the grid. Factored out
/// of <see cref="GrinderViewFactory"/> so the translation from logical edge
/// data to world position/rotation can be unit-tested and reused by gizmo
/// drawers, debug overlays, or future SFX anchors.
///
/// Conventions:
/// <list type="bullet">
///   <item>Cell (0, 0) lives at local origin (0, 0, 0).</item>
///   <item>Grinders sit one full cell outside the grid on their edge so they do not overlap ground tiles.</item>
///   <item>Rotation faces the grinder's forward (+Z) into the grid, which is the direction blocks slide when being consumed.</item>
/// </list>
/// </summary>
public static class GrinderPlacement
{
    public static void GetPose(
        GridEdge edge,
        int position,
        int width,
        GridSize gridSize,
        CellSpace cellSpace,
        out Vector3 worldPos,
        out Quaternion worldRot)
    {
        if (cellSpace == null)
        {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            return;
        }

        float cs = cellSpace.CellSize;
        // Midpoint of the grinder's footprint along the edge axis.
        float alongAxisCenter = (position + (width - 1) * 0.5f) * cs;

        switch (edge)
        {
            case GridEdge.Top:
                worldPos = new Vector3(alongAxisCenter, 0f, gridSize.height * cs);
                worldRot = Quaternion.Euler(0f, 180f, 0f);
                break;

            case GridEdge.Bottom:
                worldPos = new Vector3(alongAxisCenter, 0f, -cs);
                worldRot = Quaternion.identity;
                break;

            case GridEdge.Left:
                worldPos = new Vector3(-cs, 0f, alongAxisCenter);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Right:
                worldPos = new Vector3(gridSize.width * cs, 0f, alongAxisCenter);
                worldRot = Quaternion.Euler(0f, -90f, 0f);
                break;

            default:
                worldPos = Vector3.zero;
                worldRot = Quaternion.identity;
                break;
        }
    }
}
