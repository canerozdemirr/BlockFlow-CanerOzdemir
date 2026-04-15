using UnityEngine;

public static class GrinderGeometry
{
    // Cells spanned along the edge's parallel axis. Used to size particles.
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

    // Cells spanned perpendicular to the edge. Sets slide distance.
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

    public static Vector3 GrinderCenterWorld(
        GrinderModel grinder, GridSize gridSize, float cellSize, Transform gridRoot)
    {
        float centerAlongEdge = (grinder.Position + (grinder.Width - 1) * 0.5f) * cellSize;
        var localPos = grinder.Edge.ToLocalPoint(centerAlongEdge, gridSize, cellSize);
        return gridRoot != null ? gridRoot.TransformPoint(localPos) : localPos;
    }
}
