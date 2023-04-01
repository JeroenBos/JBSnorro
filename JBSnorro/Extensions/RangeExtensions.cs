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
}
