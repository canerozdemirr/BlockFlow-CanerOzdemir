using System;
using UnityEngine;

/// <summary>
/// Integer cell coordinate on the puzzle grid. Value type so it can be passed
/// around and stored in collections without allocations. Equality, hashing and
/// arithmetic are provided so the type drops into HashSet/Dictionary hot paths.
/// </summary>
[Serializable]
public struct GridCoord : IEquatable<GridCoord>
{
    public int x;
    public int y;

    public GridCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static GridCoord Zero  => new GridCoord(0, 0);
    public static GridCoord Up    => new GridCoord(0, 1);
    public static GridCoord Down  => new GridCoord(0, -1);
    public static GridCoord Left  => new GridCoord(-1, 0);
    public static GridCoord Right => new GridCoord(1, 0);

    public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.x + b.x, a.y + b.y);
    public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.x - b.x, a.y - b.y);
    public static GridCoord operator *(GridCoord a, int scalar)  => new GridCoord(a.x * scalar, a.y * scalar);
    public static bool operator ==(GridCoord a, GridCoord b)     => a.x == b.x && a.y == b.y;
    public static bool operator !=(GridCoord a, GridCoord b)     => !(a == b);

    public bool Equals(GridCoord other) => x == other.x && y == other.y;
    public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

    public override int GetHashCode()
    {
        // Two large primes keep collisions low for typical small-grid indices.
        unchecked { return x * 73856093 ^ y * 19349663; }
    }

    public override string ToString() => $"({x},{y})";

    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
    public static GridCoord FromVector2Int(Vector2Int v) => new GridCoord(v.x, v.y);
}
