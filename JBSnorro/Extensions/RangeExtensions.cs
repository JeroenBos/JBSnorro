using JBSnorro.Diagnostics;

namespace JBSnorro.Extensions;

public static class RangeExtensions
{
    public static Range SubRange(this Range range, Range subrange)
    {
        Index start;
        if (subrange.Start.IsFromEnd)
        {
            if (range.End.IsFromEnd)
            {
                start = new Index(subrange.Start.Value + range.End.Value, fromEnd: true);
            }
            else
            {
                start = range.End.Value - subrange.Start.Value;
            }
        }
        else
        {
            if (range.Start.IsFromEnd)
            {
                Contract.Requires<ArgumentOutOfRangeException>(range.Start.Value > subrange.Start.Value);
                start = new Index(range.Start.Value - subrange.Start.Value, fromEnd: true);
            }
            else
            {
                start = range.Start.Value + subrange.Start.Value;
            }
        }

        Index end;
        if (subrange.End.IsFromEnd)
        {
            if (range.End.IsFromEnd)
            {
                end = new Index(range.End.Value + subrange.End.Value, fromEnd: true);
            }
            else
            {
                Contract.Requires<ArgumentOutOfRangeException>(range.End.Value > subrange.End.Value);
                end = range.End.Value - subrange.End.Value;
            }
        }
        else
        {
            if (range.Start.IsFromEnd)
            {
                end = new Index(range.Start.Value - subrange.End.Value, fromEnd: true);
            }
            else
            {
                end = range.Start.Value + subrange.End.Value;
            }
        }

        return new Range(start, end);
    }
    /// <summary>
    /// Gets whether the specified index is in the specified range.
    /// </summary>
    public static bool Contains(this Range range, int index)
    {
        return range.Contains(index, false);
    }
    /// <summary>
    /// Gets whether the specified index is in the specified range.
    /// </summary>
    public static bool Contains(this Range range, int index, bool endInclusive)
    {
        Contract.Requires(!range.Start.IsFromEnd);
        Contract.Requires(!range.End.IsFromEnd);

        if (endInclusive)
        {
            return range.Start.Value <= index && index <= range.End.Value;
        }
        else
        {
            return range.Start.Value <= index && index < range.End.Value;
        }
    }
    /// <summary>
    /// Gets the length of the specified range in a collection of the specified length.
    /// </summary>
    public static int GetLength(this Range range, int length)
    {
        return range.GetOffsetAndLength(length).Length;
    }

    /// <summary> 
    /// Select a value for each integer in the specified range.
    /// </summary>
    /// <param name="range">The range to map to values. Indices from end are not allowed.</param>
    /// <exception cref="T:System.ArgumentException">An index in <paramref name="range"/> is from the end.</exception>
    /// <returns>An enumerable yielding the selected values, computed on-demand. </returns>
    public static IEnumerable<T> Select<T>(this Range range, Func<int, T> selector)
    {
        if (range.Start.IsFromEnd) throw new ArgumentException("range.Start.IsFromEnd", nameof(range));
        if (range.End.IsFromEnd) throw new ArgumentException("range.End.IsFromEnd", nameof(range));
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        return Enumerable.Range(range.Start.Value, range.End.Value - range.Start.Value).Select(selector);
    }
    /// <summary> 
    /// Select a value for each integer in the specified range.
    /// </summary>
    /// <param name="range">The range to map to values. Indices from end are not allowed.</param>
    /// <exception cref="T:System.ArgumentException">An index in <paramref name="range"/> is from the end.</exception>
    /// <returns>An array of the selected values, computed eagerly. </returns>
    public static T[] Map<T>(this Range range, Func<int, T> selector)
    {
        if (range.Start.IsFromEnd) throw new ArgumentException("range.Start.IsFromEnd", nameof(range));
        if (range.End.IsFromEnd) throw new ArgumentException("range.End.IsFromEnd", nameof(range));
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        return Enumerable.Range(range.Start.Value, range.End.Value - range.Start.Value).Select(selector).ToArray(range.End.Value - range.Start.Value);
    }
}
