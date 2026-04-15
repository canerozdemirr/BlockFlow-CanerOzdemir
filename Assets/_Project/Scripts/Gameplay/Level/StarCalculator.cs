// 3 stars above 2/3 remaining, 2 above 1/3, 1 otherwise. No timer (total<=0) grants max.
public static class StarCalculator
{
    public const int MaxStars = 3;

    public static int FromTimeRemaining(float remaining, float total)
    {
        if (total <= 0f) return MaxStars;
        float ratio = remaining / total;
        if (ratio > 0.66f) return 3;
        if (ratio > 0.33f) return 2;
        return 1;
    }
}
