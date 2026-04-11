using UnityEngine;

/// <summary>
/// Pure ray-to-grid helper. Casts a screen point onto the grid's ground plane
/// and reports the cell it landed in (plus the block occupying that cell, if
/// any). Physics-free on purpose: blocks and ground tiles never need colliders
/// because the simulation is authoritative, and skipping Physics.Raycast is a
/// real win on mobile.
///
/// Used by <see cref="DragController"/> to hit-test block pickups and to
/// translate finger positions into grid-local deltas during a drag.
/// </summary>
public static class GridPicker
{
    /// <summary>
    /// Projects <paramref name="screenPos"/> onto the XZ plane at the grid
    /// root's world height and writes the local-space hit point. Returns
    /// false if the ray diverges from the plane (e.g. camera pointing away).
    /// </summary>
    public static bool TryGetLocalHit(
        Vector2 screenPos,
        Camera camera,
        Transform gridRoot,
        out Vector3 localHit)
    {
        localHit = default;
        if (camera == null) return false;

        var ray = camera.ScreenPointToRay(screenPos);

        // Plane sits at the grid root's world Y, oriented by its up vector.
        var planeOrigin = gridRoot != null ? gridRoot.position : Vector3.zero;
        var planeNormal = gridRoot != null ? gridRoot.up       : Vector3.up;
        var plane = new Plane(planeNormal, planeOrigin);

        if (!plane.Raycast(ray, out float distance)) return false;
        if (distance < 0f) return false;

        var worldHit = ray.GetPoint(distance);
        localHit = gridRoot != null ? gridRoot.InverseTransformPoint(worldHit) : worldHit;
        return true;
    }

    /// <summary>
    /// Converts a local-space hit to the owning grid cell and looks up its
    /// occupant, if any. Returns false when the cell is outside the grid or
    /// empty — the out parameters still hold meaningful values (cell is
    /// always populated so callers can debug the miss).
    /// </summary>
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
