using UnityEngine;

// Physics-free on purpose: simulation is authoritative, and skipping
// Physics.Raycast is a real win on mobile.
public static class GridPicker
{
    public static bool TryGetLocalHit(
        Vector2 screenPos,
        Camera camera,
        Transform gridRoot,
        out Vector3 localHit)
    {
        localHit = default;
        if (camera == null) return false;

        var ray = camera.ScreenPointToRay(screenPos);

        var planeOrigin = gridRoot != null ? gridRoot.position : Vector3.zero;
        var planeNormal = gridRoot != null ? gridRoot.up       : Vector3.up;
        var plane = new Plane(planeNormal, planeOrigin);

        if (!plane.Raycast(ray, out float distance)) return false;
        if (distance < 0f) return false;

        var worldHit = ray.GetPoint(distance);
        localHit = gridRoot != null ? gridRoot.InverseTransformPoint(worldHit) : worldHit;
        return true;
    }

    public static bool TryPick(
        Vector3 localHit,
        CellSpace cellSpace,
        GridModel grid,
        out GridCoord cell,
        out BlockId blockId)
    {
        blockId = BlockId.None;
        cell = cellSpace != null ? cellSpace.ToGrid(localHit) : default;

        if (grid == null) return false;
        if (!grid.IsInside(cell)) return false;

        blockId = grid.GetOccupant(cell);
        return blockId.IsValid;
    }
}
