using System;

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

    public bool Contains(GridCoord coord) =>
        coord.x >= 0 && coord.x < width &&
        coord.y >= 0 && coord.y < height;

    public override string ToString() => $"{width}x{height}";
}
