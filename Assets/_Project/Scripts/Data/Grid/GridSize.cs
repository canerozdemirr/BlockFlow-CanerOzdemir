using System;

/// <summary>
/// Dimensions of a rectangular grid in cells. Serializable so designers can
/// edit it in the inspector, and immutable-in-spirit: treated as a value.
/// </summary>
[Serializable]
public struct GridSize
{
    public int width;
    public int height;

    public GridSize(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public int Area => width * height;

    /// <summary>
    /// True if the given coordinate lies inside the [0, width) x [0, height) box.
    /// </summary>
    public bool Contains(GridCoord coord) =>
        coord.x >= 0 && coord.x < width &&
        coord.y >= 0 && coord.y < height;

    public override string ToString() => $"{width}x{height}";
}
