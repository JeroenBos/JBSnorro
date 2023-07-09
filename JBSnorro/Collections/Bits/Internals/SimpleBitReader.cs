#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;

namespace JBSnorro.Collections.Bits.Internals;

/// <summary>
/// A bit reader that reads floating point numbers assuming they're particularly encoded.
/// </summary>
internal class SimpleBitReader : BitReader
{
    public double Min { get; }
    public double Max { get; }

    public SimpleBitReader(BitArrayReadOnlySegment data, double min, double max)
        : base(data)
    {
        Min = min;
        Max = max;
    }
    public SimpleBitReader(BitArray data, double min, double max, ulong startBitIndex = 0)
        : base(data, startBitIndex)
    {
        Min = min;
        Max = max;
    }
    public SimpleBitReader(BitArray data, double min, double max, ulong startBitIndex, ulong length)
        : base(data, startBitIndex, length)
    {
        Min = min;
        Max = max;
    }
    public SimpleBitReader(ulong[] data, double min, double max, int dataBitCount, int startBitIndex = 0)
        : base(data, dataBitCount, startBitIndex)
    {
        Min = min;
        Max = max;
    }

    public override IBitReader this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)Length));
            return new SimpleBitReader(data, Min, Max, startOffset + (ulong)offset, (ulong)length);
        }
    }

    public override double ReadDouble(int bitCount)
    {
        Contract.Requires((1..64).Contains(bitCount, endInclusive: true));

        decimal bits = ReadUInt64(bitCount);
        ulong rangeLength = 2UL << bitCount - 1;
        decimal rangeScale = (decimal)(Max - Min) / rangeLength;
        double result = (double)(bits * rangeScale + (decimal)Min);
        return result;
    }

    public override SimpleBitReader Clone()
    {
        // base.current is dealt with through startOffset
        return new SimpleBitReader(data, Min, Max, startOffset, Length);
    }
}
