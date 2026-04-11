/// <summary>
/// Cardinal direction a block can be pushed along. Kept as a plain enum so it
/// is trivially JSON/inspector friendly and branches cheaply.
/// </summary>
public enum GridDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class GridDirectionExtensions
{
    /// <summary>
    /// Converts a cardinal direction into its unit delta on the grid.
    /// </summary>
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
