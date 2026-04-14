/// <summary>
/// Pure star-rating rule for a completed level. Kept out of the view so the
/// "how many stars for this time?" decision is a single testable function
/// rather than business logic buried in a popup.
///
/// Rule: 3 stars above 2/3 time remaining, 2 above 1/3, 1 otherwise.
/// If the total is non-positive (no timer configured), grant the max.
/// </summary>
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
