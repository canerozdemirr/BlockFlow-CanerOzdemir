using UnityEngine;

public static class GridEdgeExtensions
{
    public static bool IsHorizontal(this GridEdge edge)
        => edge == GridEdge.Top || edge == GridEdge.Bottom;

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

    // Returns the cell component parallel to the edge: x for Top/Bottom, y for Left/Right.
    public static int AlongAxis(this GridEdge edge, GridCoord cell)
        => edge.IsHorizontal() ? cell.x : cell.y;

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

    public static bool TryParse(string edge, out GridEdge result)
        => System.Enum.TryParse(edge, ignoreCase: false, out result);

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
