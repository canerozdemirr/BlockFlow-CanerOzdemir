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
///   <item>Grinders are positioned flush with the grid boundary (half a cell beyond the outermost cell centers) so they sit right at the edge of the ground tiles.</item>
///   <item>Rotation is an edge-specific <b>delta</b> (not absolute). The factory multiplies it by the prefab's authored base rotation so the designer's FBX orientation fix is always preserved.</item>
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
        float half = cs * 0.5f;

        // Midpoint of the grinder's footprint along the edge axis.
        float alongAxisCenter = (position + (width - 1) * 0.5f) * cs;

        // Perpendicular position: flush with the grid boundary.
        // Cell centers run from 0 to (size-1)*cs. Each cell extends ±half
        // around its center, so the grid boundary is at -half and
        // (size-1)*cs + half = (size - 0.5)*cs.
        switch (edge)
        {
            case GridEdge.Top:
                worldPos = new Vector3(alongAxisCenter, 0f, (gridSize.height - 0.5f) * cs);
                worldRot = Quaternion.Euler(0f, 180f, 0f);
                break;

            case GridEdge.Bottom:
                worldPos = new Vector3(alongAxisCenter, 0f, -half);
                worldRot = Quaternion.identity;
                break;

            case GridEdge.Left:
                worldPos = new Vector3(-half, 0f, alongAxisCenter);
                worldRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case GridEdge.Right:
                worldPos = new Vector3((gridSize.width - 0.5f) * cs, 0f, alongAxisCenter);
                worldRot = Quaternion.Euler(0f, -90f, 0f);
                break;

            default:
                worldPos = Vector3.zero;
                worldRot = Quaternion.identity;
                break;
        }
    }
}
