using UnityEngine;

/// <summary>
/// Pure geometry helpers for grinder-facing math: block extents along each
/// axis, edge-local-to-world conversion, and the point where a block meets a
/// grinder. Separated from <see cref="GrinderService"/> so consumption logic
/// doesn't carry the Vector3 math and the math is trivially testable.
/// </summary>
public static class GrinderGeometry
{
    /// <summary>Cells spanned along the edge's parallel axis (sizes particles).</summary>
    public static int BlockParallelExtent(BlockModel block, GridEdge edge)
    {
        var offsets = block.CellOffsets;
        if (offsets == null || offsets.Length == 0) return 1;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            int val = edge.AlongAxis(offsets[i]);
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return max - min + 1;
    }

    /// <summary>Cells spanned perpendicular to the edge (sets slide distance).</summary>
    public static float BlockPerpendicularExtent(BlockModel block, GridEdge edge)
    {
        var offsets = block.CellOffsets;
        if (offsets == null || offsets.Length == 0) return 1f;

        bool perpIsX = !edge.IsHorizontal();
        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            int val = perpIsX ? offsets[i].x : offsets[i].y;
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return max - min + 1;
    }

    /// <summary>World-space point at the edge midway along the block's span.</summary>
    public static Vector3 BlockEdgeCenterWorld(
        BlockModel block, GrinderModel grinder, GridSize gridSize,
        float cellSize, Transform gridRoot)
    {
        var offsets = block.CellOffsets;
        var origin = block.Origin;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            int val = grinder.Edge.AlongAxis(origin + offsets[i]);
            if (val < min) min = val;
            if (val > max) max = val;
        }

        float centerAlongEdge = (min + max) * 0.5f * cellSize;
        var localPos = grinder.Edge.ToLocalPoint(centerAlongEdge, gridSize, cellSize);
        return gridRoot != null ? gridRoot.TransformPoint(localPos) : localPos;
    }

    /// <summary>World-space center of the grinder's full coverage area.</summary>
    public static Vector3 GrinderCenterWorld(
        GrinderModel grinder, GridSize gridSize, float cellSize, Transform gridRoot)
    {
        float centerAlongEdge = (grinder.Position + (grinder.Width - 1) * 0.5f) * cellSize;
        var localPos = grinder.Edge.ToLocalPoint(centerAlongEdge, gridSize, cellSize);
        return gridRoot != null ? gridRoot.TransformPoint(localPos) : localPos;
    }
}
