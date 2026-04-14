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
