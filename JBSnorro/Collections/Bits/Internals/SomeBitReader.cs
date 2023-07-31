#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;

namespace JBSnorro.Collections.Bits.Internals;

/// <summary>
/// A bit reader that reads floating point numbers assuming they're particularly encoded.
/// </summary>
internal class SomeBitReader : BitReader
{
    public SomeBitReader(BitArrayReadOnlySegment data)
        : base(data)
    {
    }
    public SomeBitReader(BitArray data, ulong startBitIndex = 0)
        : base(data, startBitIndex)
    {
    }
    public SomeBitReader(BitArray data, ulong startBitIndex, ulong length)
        : base(data, startBitIndex, length)
    {
    }
    public SomeBitReader(ulong[] data, int dataBitCount, int startBitIndex = 0)
        : base(data, dataBitCount, startBitIndex)
    {
    }

    public override IBitReader Clone()
    {
        // this.current is dealt with through startOffset
        return new SomeBitReader(data, startOffset, Length);
    }
    public override IBitReader this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)Length));
            return new SomeBitReader(data, startOffset + (ulong)offset, (ulong)length);
        }
    }
}
