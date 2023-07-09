#nullable enable
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DebuggerDisplayAttribute = System.Diagnostics.DebuggerDisplayAttribute;
namespace JBSnorro.Collections;

public interface IBitReader
{
    ulong Length { get; }
    ulong RemainingLength => checked(Length - Position);
    ulong Position { get; }


    bool CanRead(int bitCount)
    {
        if (bitCount < 0) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return CanRead((ulong)bitCount);
    }
    bool CanRead(ulong bitCount)
    {
        return RemainingLength >= bitCount;
    }
    void Seek(ulong bitIndex);
    long IndexOf(ulong item, int itemLength) => IndexOf(item, itemLength, this.Position);

    IBitReader Clone();
    /// <param name="range">Relative to the complete bit array, not relative to the remaining part.</param>
    IBitReader this[Range range] { get; }


    public bool ReadBit()
    {
        if (this.RemainingLength < 1)
            throw new InsufficientBitsException("bit");

        ulong result = this.ReadUInt64(1);
        return result != 0;
    }
    [DebuggerHidden]
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("byte");

        return (byte)ReadUInt64(bitCount);
    }
    [DebuggerHidden]
    public sbyte ReadSByte(int bitCount = 8)
    {
        if (bitCount < 2 || bitCount > 8)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("sbyte");

        return (sbyte)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 16)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("short");

        return (short)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public ushort ReadUInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("ushort");

        return (ushort)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (int)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public uint ReadUInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (uint)ReadUInt64(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("long");

        bool sign = this.ReadUInt64(1) == 0;
        var magnitude = (long)this.ReadUInt64(bitCount - 1);
        if (sign)
        {
            return magnitude;
        }
        else
        {
            return -magnitude - 1; // -1, otherwise 0 is mapped doubly
        }
    }
    ulong ReadUInt64(int bitCount = 64);
    [DebuggerHidden]
    public Half ReadHalf(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("Half");

        // half has 5 bits exponent
        return (Half)ReadDouble(bitCount);
    }
    [DebuggerHidden]
    public float ReadSingle(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("float");

        // float has 8 bits exponent
        return (float)ReadDouble(bitCount);
    }
    public double ReadDouble(int bitCount = 64);
    /// <summary>
    /// Gets all indices in the stream the pattern occurs at.
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= this.Length);

        var nextBitIndex = startBitIndex;
        while (true)
        {
            this.Seek(nextBitIndex);
            long index = this.IndexOf(item, itemLength);
            if (index == -1)
                yield break;
            yield return index;
            nextBitIndex = checked((ulong)index + (ulong)itemLength);
        }

    }

    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    /// <returns>-1 if not found.</returns>
    [DebuggerHidden]
    long IndexOf(ulong item, int itemLength, ulong startBitIndex)
    {
        return IndexOfImpl(this, item, itemLength, startBitIndex);
    }
    internal static long IndexOfImpl(IBitReader @this, ulong item, int itemLength, ulong startBitIndex)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= @this.Length);
        Contract.Requires<NotImplementedException>(itemLength <= 64);

        @this.Seek(startBitIndex);
        return @this.IndexOf(item, itemLength);
    }
}

[DebuggerDisplay("{ToDebuggerDisplay()}")]
public abstract class BitReader : IBitReader
{
    protected readonly BitArray data;
    /// <summary>
    /// The index where this reader actually starts. Cannot be sought beyond.
    /// Treat this BitReader as if the stream really starts at <see cref="startOffset"/>.
    /// </summary>
    protected readonly ulong startOffset;
    /// <summary> In bits. </summary>
    protected ulong current;
    /// <summary> 
    /// Gets the length of the stream this <see cref="IBitReader"/> can read, in bits.
    /// </summary>
    /// <remarks>Does not count the bits before the startOffset</remarks>
    public ulong Length { get; }

    private ulong End
    {
        get => startOffset + Length;
    }
    /// <summary> In bits.</summary>
    public ulong RemainingLength
    {
        get => this.End - current;
    }
    /// <summary>
    /// Gets the current position of this reader in the bit stream.
    /// </summary>
    public ulong Position
    {
        get => current - startOffset;
    }
    /// <param name="range">Relative to the complete bit array, not relative to the remaining part.</param>
    public abstract IBitReader this[Range range] { get; }

    /// <summary>
    /// Gets the remainder of the bits in a segment.
    /// </summary>
    public BitArrayReadOnlySegment RemainingSegment
    {
        get => this.data[checked((int)this.current)..];
    }
    [DebuggerHidden]
    public BitReader(BitArray data, ulong startBitIndex = 0)
        : this(data, startBitIndex, data.Length - startBitIndex)
    {
    }
    [DebuggerHidden]
    public BitReader(BitArray data, ulong startBitIndex, ulong length)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (startBitIndex + length > data.Length) throw new ArgumentOutOfRangeException(nameof(startBitIndex));

        this.data = data;
        this.startOffset = startBitIndex;
        this.current = startBitIndex;
        this.Length = length;
    }
    [DebuggerHidden]
    /// <param name="dataBitCount"> The length of the number of bits in <see cref="data"/>, including those to be excluded before <see cref="startBitIndex"/></param>
    public BitReader(ulong[] data, int dataBitCount, int startBitIndex = 0)
        : this(data, dataBitCount.ToULong(), startBitIndex.ToULong())
    {
    }
    [DebuggerHidden]
    /// <param name="dataBitCount"> The length of the number of bits in <see cref="data"/>, including those to be excluded before <see cref="startBitIndex"/></param>
    public BitReader(ulong[] data, ulong dataBitCount, ulong startBitIndex = 0)
    {
        this.data = BitArray.FromRef(data, dataBitCount);
        this.current = startBitIndex;
        this.Length = dataBitCount - startBitIndex;
    }
    [DebuggerHidden]
    public BitReader(BitArrayReadOnlySegment data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        this.data = data.data;
        this.startOffset = data.start;
        this.current = startOffset;
        this.Length = data.Length;
    }

    /// <summary>
    /// Returns whether this reader still has the specified number of bits to read.
    /// </summary>
    public bool CanRead(int bitCount)
    {
        if (bitCount < 0 || bitCount > 64) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return RemainingLength > (ulong)bitCount;
    }
    /// <summary>
    /// Returns whether this reader still has the specified number of bits to read.
    /// </summary>
    public bool CanRead(ulong bitCount)
    {
        if (bitCount > 64) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return RemainingLength >= bitCount;
    }

    /// <remarks>Is sealed because of <see cref="ReadUInt64(BitArray, ulong, int)"/></remarks>
    public ulong ReadUInt64(int bitCount = 64)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if ((ulong)bitCount > this.RemainingLength)
            throw new ArgumentException("Not enough remaining length", nameof(bitCount));

        ulong bits = this.data.GetULong(this.current);
        ulong mask = BitArrayExtensions.LowerBitsMask(bitCount);
        ulong result = bits & mask;

        current += (ulong)bitCount;

        return result;
    }
    public virtual double ReadDouble(int bitCount = 64)
    {
        throw new NotSupportedException("If you want to read floating point numbers, use ony of the derived types");
    }

    public void Seek(ulong bitIndex)
    {
        Contract.Requires<ArgumentOutOfRangeException>(0 <= bitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(bitIndex <= this.Length);

        this.current = bitIndex;
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    /// <returns>-1 if not found.</returns>
    public long IndexOf(ulong item, int itemLength = 64)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);

        return this.data.IndexOf(item, itemLength, this.Position);
    }


    /// <summary>
    /// <inheritdoc cref="IBitReader.IndicesOf(ulong, int, ulong)"/>
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        IBitReader self = this;
        return self.IndicesOf(item, itemLength, startBitIndex);
    }

    /// <param name="length"> In bits. </param>
    public void CopyTo(ulong[] dest, ulong startBitIndex, ulong length, int destBitIndex)
    {
        if (dest is null)
            throw new ArgumentNullException(nameof(dest));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (startBitIndex < 0 || startBitIndex + length > this.Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (destBitIndex < 0 || (ulong)destBitIndex + length > (ulong)dest.Length * 64UL)
            throw new ArgumentOutOfRangeException(nameof(destBitIndex));

        this.data.CopyTo(dest, destBitIndex);
    }
    public abstract IBitReader Clone();


    protected virtual string ToDebuggerDisplay()
    {
        return $"{this.GetType().Name}({startOffset}..[|{current}|]..{End}, Length={this.Length}/{this.data.Length}, Remaining={this.RemainingLength})";
    }

    /// <summary>
    /// It doesn't matter which derivative type you use, if you're only going to read UInt64s: the implementations of ReadUInt64 don't differ
    /// </summary>
    internal static ulong ReadUInt64(BitArray data, ulong startIndex, int dataLength)
    {
        return new SomeBitReader(data, startIndex).ReadUInt64(dataLength);
    }
    /// <summary>
    /// This is the function that creates <see cref="BitReader"/>s, and can be overridden.
    /// This is the only place where I make the decision to default to <see cref="SomeBitReader"/> of all the <see cref="IBitReader"/>.
    /// I.e. <see cref="BitArray.ToBitReader()"/> and <see cref="BitArrayReadOnlySegment.ToBitReader(ulong)"/> defer to this.
    /// 
    /// </summary>
    public static Func<BitArray /*data*/, ulong /*startBitIndex*/, ulong /*length*/, BitReader> Create = (data, startBitIndex, length) => new SomeBitReader(data, startBitIndex, length);
}

/// <summary>
/// A bit reader that reads floating point numbers assuming they're particularly encoded.
/// </summary>
public class SomeBitReader : BitReader
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

    public override double ReadDouble(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("double");

        // the idea is that if the mantissa is filled, then the bits are just becoming less and less relevant
        // but before some bitCount, there simply isn't enough bits to have this strategy. Something more complicated (or simpler) is needed
        ulong originalRemainingLength = this.RemainingLength;
        int significantBitCount = Math.Max(2, bitCount / 2);
        int exponentBitCount = bitCount - significantBitCount;

        IBitReader self = this;
        var significant = self.ReadInt64(significantBitCount);
        Contract.Assert(this.RemainingLength + (ulong)significantBitCount == originalRemainingLength);
        var exponent = exponentBitCount == 0 ? 1 : exponentBitCount == 1 ? (double)this.ReadUInt64(exponentBitCount) : self.ReadInt64(exponentBitCount);

        var value = 2 * significant * double.Pow(2, exponent);
        return value;
    }

    public override IBitReader Clone()
    {
        // this.current is dealt with through startOffset
        return new SomeBitReader(this.data, this.startOffset, this.Length);
    }
    public override IBitReader this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)this.Length));
            return new SomeBitReader(this.data, this.startOffset + (ulong)offset, (ulong)length);
        }
    }
}


/// <summary>
/// A bit reader that reads floating point numbers assuming they're particularly encoded.
/// </summary>
public class FloatingPointBitReader : BitReader
{
    public double Min { get; }
    public double Max { get; }

    public FloatingPointBitReader(BitArrayReadOnlySegment data, double min, double max)
        : base(data)
    {
        this.Min = min;
        this.Max = max;
    }
    public FloatingPointBitReader(BitArray data, double min, double max, ulong startBitIndex = 0)
        : base(data, startBitIndex)
    {
        this.Min = min;
        this.Max = max;
    }
    public FloatingPointBitReader(BitArray data, double min, double max, ulong startBitIndex, ulong length)
        : base(data, startBitIndex, length)
    {
        this.Min = min;
        this.Max = max;
    }
    public FloatingPointBitReader(ulong[] data, double min, double max, int dataBitCount, int startBitIndex = 0)
        : base(data, dataBitCount, startBitIndex)
    {
        this.Min = min;
        this.Max = max;
    }

    public override IBitReader this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)this.Length));
            return new FloatingPointBitReader(this.data, this.Min, this.Max, this.startOffset + (ulong)offset, (ulong)length);
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

    public override FloatingPointBitReader Clone()
    {
        // base.current is dealt with through startOffset
        return new FloatingPointBitReader(this.data, this.Min, this.Max, this.startOffset, this.Length);
    }
}
