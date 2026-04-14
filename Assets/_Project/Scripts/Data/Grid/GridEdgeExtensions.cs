using UnityEngine;

/// <summary>
/// Axis and direction queries on <see cref="GridEdge"/>. Centralizes enum
/// switches that were previously scattered across grinder/grid code so new
/// edge-dependent logic only needs one place updated (closed-for-modification).
/// </summary>
public static class GridEdgeExtensions
{
    /// <summary>Top/Bottom run along the X axis; Left/Right along Y.</summary>
    public static bool IsHorizontal(this GridEdge edge)
        => edge == GridEdge.Top || edge == GridEdge.Bottom;

    /// <summary>
    /// True if the cell sits on the outer row/column that belongs to this edge.
    /// </summary>
    public static bool ContainsCell(this GridEdge edge, GridCoord cell, GridSize size)
    {
        switch (edge)
        {
            case GridEdge.Top:    return cell.y == size.height - 1;
            case GridEdge.Bottom: return cell.y == 0;
            case GridEdge.Left:   return cell.x == 0;
            case GridEdge.Right:  return cell.x == size.width - 1;
        }
        return false;
    }

    /// <summary>
    /// Returns the cell component that runs parallel to this edge — x for
    /// Top/Bottom, y for Left/Right. Used to test coverage ranges and compute
    /// along-edge extents without repeating the edge switch.
    /// </summary>
    public static int AlongAxis(this GridEdge edge, GridCoord cell)
        => edge.IsHorizontal() ? cell.x : cell.y;

    /// <summary>
    /// Converts a point specified by its along-edge local offset into a
    /// grid-local Vector3 at the given edge, raised on Y so effects render
    /// above the tiles. Caller applies the grid root transform for world space.
    /// </summary>
    public static Vector3 ToLocalPoint(this GridEdge edge, float alongEdgeLocal, GridSize gridSize, float cellSize, float y = 0.3f)
    {
        switch (edge)
        {
            case GridEdge.Right:  return new Vector3((gridSize.width - 0.5f) * cellSize, y, alongEdgeLocal);
            case GridEdge.Left:   return new Vector3(-0.5f * cellSize, y, alongEdgeLocal);
            case GridEdge.Top:    return new Vector3(alongEdgeLocal, y, (gridSize.height - 0.5f) * cellSize);
            case GridEdge.Bottom: return new Vector3(alongEdgeLocal, y, -0.5f * cellSize);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Parses an edge name (Top/Bottom/Left/Right, case-sensitive) into its enum.
    /// Returns false for null/empty/unknown — callers decide how to handle.
    /// </summary>
    public static bool TryParse(string edge, out GridEdge result)
        => System.Enum.TryParse(edge, ignoreCase: false, out result);

    /// <summary>
    /// World-space direction a block slides when consumed by a grinder on this edge.
    /// </summary>
    public static Vector3 ToSlideDirection(this GridEdge edge)
    {
        switch (edge)
        {
            case GridEdge.Top:    return Vector3.forward;
            case GridEdge.Bottom: return Vector3.back;
            case GridEdge.Left:   return Vector3.left;
            case GridEdge.Right:  return Vector3.right;
        }
        return Vector3.forward;
    }
}
