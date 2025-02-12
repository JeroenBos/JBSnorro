namespace JBSnorro;

public static class NumberExtensions
{
    /// <summary>
    /// Gets whether this is nonnegative.
    /// </summary>
    public static bool IsNonNegative(this int i)
    {
        return i >= 0;
    }
    /// <summary>
    /// Gets whether this is positive.
    /// </summary>
    public static bool IsPositive(this int i)
    {
        return i > 0;
    }
    /// <summary>
    /// Gets whether this is a nonnegative real number.
    /// </summary>
    public static bool IsNonNegativeFinite(this float f)
    {
        return float.IsRealNumber(f) && f >= 0 && !float.IsPositiveInfinity(f);
    }
    /// <summary>
    /// Gets whether this is a positive real number.
    /// </summary>
    public static bool IsPositiveFinite(this float f)
    {
        return float.IsRealNumber(f) && f > 0 && !float.IsPositiveInfinity(f);
    }
    /// <summary>
    /// Gets whether this is a negative real number.
    /// </summary>
    public static bool IsNegativeFinite(this float f)
    {
        return float.IsRealNumber(f) && f > 0 && !float.IsNegativeInfinity(f);
    }
    /// <summary>
    /// Gets whether this is a real number.
    /// </summary>
    public static bool IsFinite(this float f)
    {
        return float.IsRealNumber(f) && !float.IsInfinity(f);
    }

    /// <summary>
    /// Gets whether this is in [0, 1].
    /// </summary>
    public static bool IsInUnitRange(this float f)
    {
        return float.IsRealNumber(f) && 0 <= f && f <= 1;
    }
    /// <summary>
    /// Gets whether this is a nonnegative real number.
    /// </summary>
    public static bool IsNonNegativeFinite(this double d)
    {
        return double.IsRealNumber(d) && d >= 0 && !double.IsPositiveInfinity(d);
    }
    /// <summary>
    /// Gets whether this is a positive real number.
    /// </summary>
    public static bool IsPositiveFinite(this double d)
    {
        return double.IsRealNumber(d) && d > 0 && !double.IsPositiveInfinity(d);
    }
    /// <summary>
    /// Gets whether this is a negative real number.
    /// </summary>
    public static bool IsNegativeFinite(this double d)
    {
        return double.IsRealNumber(d) && d > 0 && !double.IsNegativeInfinity(d);
    }
    /// <summary>
    /// Gets whether this is a real number.
    /// </summary>
    public static bool IsFinite(this double d)
    {
        return double.IsRealNumber(d) && !double.IsInfinity(d);
    }

    /// <summary>
    /// Gets whether this is in [0, 1].
    /// </summary>
    public static bool IsInUnitRange(this double d)
    {
        return double.IsRealNumber(d) && 0 <= d && d <= 1;
    }
}
