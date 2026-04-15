public enum GridDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class GridDirectionExtensions
{
    public static GridCoord ToDelta(this GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Up:    return GridCoord.Up;
            case GridDirection.Down:  return GridCoord.Down;
            case GridDirection.Left:  return GridCoord.Left;
            case GridDirection.Right: return GridCoord.Right;
            default:                  return GridCoord.Zero;
        }
    }
}
