using JBSnorro.SystemTypes;

namespace JBSnorro;

public static class FloatingTypeEqualityComparisonHelper
{
    private const float tolerance = 0.0001f;
    /// <summary> Determines whether the two specified floating point numbers are equal (up to an optionally specified tolerance). </summary>
    public static bool ApproximatelyEquals(this float a, float b, float tolerance = tolerance)
    {
        return ((double)a).ApproximatelyEquals(b, tolerance);
    }

    /// <summary> Determines whether the two specified floating point numbers are equal (up to an optionally specified tolerance). </summary>
    public static bool ApproximatelyEquals(this double a, double b, double tolerance = tolerance)
    {
        if (double.IsNaN(a))
            return double.IsNaN(b);
        else if (double.IsNaN(b))
            return false;
        if (double.IsInfinity(a))
            if (double.IsPositiveInfinity(a))
                return double.IsPositiveInfinity(b);
            else
                return double.IsNegativeInfinity(b);

        else if (double.IsInfinity(b))
            return false;

        return Math.Abs(a - b) < tolerance;
    }
    /// <summary> Determines whether the coordinates of the two specified points are equal (up to an optionally specified tolerance). </summary>
    public static bool ApproximatelyEquals(this Point a, Point b, double tolerance = tolerance)
    {
        return ApproximatelyEquals(a.X, b.X, tolerance) && ApproximatelyEquals(a.Y, b.Y, tolerance);
    }
    /// <summary> Determines whether the specified value is in the specified interval, or sufficiently close to it. </summary>
    /// <param name="value"> The value to be checked that it is in the specified interval. </param>
    /// <param name="boundary1"> One boundary of the interval. </param>
    /// <param name="boundary2"> Another boundary of the interval. </param>
    /// <param name="tolerance"> The maximum distance for a value to the interval, such that the value considered close enough to the interval to be contained in it. </param>
    public static bool ApproximatelyInInterval(this double value, double boundary1, double boundary2, double tolerance = tolerance)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new NotImplementedException();
        double min = Math.Min(boundary1, boundary2);
        double max = Math.Max(boundary1, boundary2);

        return min - tolerance < value && value < max + tolerance;
    }
}
