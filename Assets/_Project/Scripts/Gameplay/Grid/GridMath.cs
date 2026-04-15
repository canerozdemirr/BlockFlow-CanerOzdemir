public static class GridMath
{
    // CCW rotation so positive values in level JSON match mathematical convention.
    public static GridCoord[] Rotate(GridCoord[] offsets, int quarterTurns)
    {
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
                case 1: result[i] = new GridCoord(-y,  x); break;
                case 2: result[i] = new GridCoord(-x, -y); break;
                case 3: result[i] = new GridCoord( y, -x); break;
            }
        }
        return result;
    }
}
