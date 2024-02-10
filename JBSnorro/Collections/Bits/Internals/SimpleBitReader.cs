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

    public override SimpleBitReader Clone()
    {
        // base.current is dealt with through startOffset
        return new SimpleBitReader(data, Min, Max, startOffset, Length);
    }
}
