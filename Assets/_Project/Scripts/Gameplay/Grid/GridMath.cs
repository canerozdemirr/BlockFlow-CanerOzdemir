/// <summary>
/// Pure grid arithmetic helpers that don't belong to any specific class. Kept
/// small on purpose — only the operations reused across the simulation.
/// </summary>
public static class GridMath
{
    /// <summary>
    /// Rotates a set of origin-relative cell offsets counter-clockwise by
    /// <paramref name="quarterTurns"/> * 90°. Returns a fresh array; the input
    /// is never mutated. Rotation direction is CCW so that a positive rotation
    /// value in level JSON maps to the mathematical convention designers
    /// generally expect.
    /// </summary>
    public static GridCoord[] Rotate(GridCoord[] offsets, int quarterTurns)
    {
        // Normalize to [0, 3], handling negatives too.
        int turns = ((quarterTurns % 4) + 4) % 4;

        int count = offsets == null ? 0 : offsets.Length;
        var result = new GridCoord[count];

        if (count == 0) return result;

        if (turns == 0)
        {
            for (int i = 0; i < count; i++) result[i] = offsets[i];
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            int x = offsets[i].x;
            int y = offsets[i].y;
            switch (turns)
            {
                case 1: result[i] = new GridCoord(-y,  x); break; //  90° CCW
                case 2: result[i] = new GridCoord(-x, -y); break; // 180°
                case 3: result[i] = new GridCoord( y, -x); break; // 270° CCW
            }
        }
        return result;
    }
}
