#nullable enable
using JBSnorro.Diagnostics;
using System.Diagnostics;
using DebuggerDisplayAttribute = System.Diagnostics.DebuggerDisplayAttribute;
namespace JBSnorro.Collections;

public interface IBitReader
{
    ulong Length { get; }
    ulong RemainingLength { get; }
    ulong Position { get; }

    ulong ReadUInt64(int bitCount = 64);

    bool CanRead(int bitCount)
    {
        if (bitCount < 0) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return CanRead((ulong)bitCount);
    }
    bool CanRead(ulong bitCount);
    void Seek(ulong bitIndex);
    long IndexOf(ulong item, int itemLength);

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
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("byte");

        return (byte)ReadUInt64(bitCount);
    }
    public sbyte ReadSByte(int bitCount = 8)
    {
        if (bitCount < 2 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("sbyte");

        return (sbyte)ReadInt64(bitCount);
    }
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("short");

        return (short)ReadInt64(bitCount);
    }
    public ushort ReadUInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("ushort");

        return (ushort)ReadInt64(bitCount);
    }
    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (int)ReadInt64(bitCount);
    }
    public uint ReadUInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (uint)ReadUInt64(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));
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
    public Half ReadHalf(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("Half");

        // half has 5 bits exponent
        return (Half)ReadDouble(bitCount);
    }
    public float ReadSingle(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("float");

        // float has 8 bits exponent
        return (float)ReadDouble(bitCount);
    }
    public double ReadDouble(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("double");

        // the idea is that if the mantissa is filled, then the bits are just becoming less and less relevant
        // but before some bitCount, there simply isn't enough bits to have this strategy. Something more complicated (or simpler) is needed
        ulong originalRemainingLength = this.RemainingLength;
        int significantBitCount = Math.Max(2, bitCount / 2);
        int exponentBitCount = bitCount - significantBitCount;

        var significant = this.ReadInt64(significantBitCount);
        Contract.Assert(this.RemainingLength + (ulong)significantBitCount == originalRemainingLength);
        var exponent = exponentBitCount == 0 ? 1 : exponentBitCount == 1 ? (double)this.ReadUInt64(exponentBitCount) : this.ReadInt64(exponentBitCount);

        var value = 2 * significant * double.Pow(2, exponent);
        return value;
    }
    /// <summary>
    /// Gets all indices in the stream the pattern occurs at.
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        return IndicesOfImpl(this, item, itemLength, startBitIndex);
    }
    internal static IEnumerable<long> IndicesOfImpl(IBitReader @this, ulong item, int itemLength, ulong startBitIndex)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= @this.Length);

        var nextBitIndex = startBitIndex;
        while (true)
        {
            @this.Seek(nextBitIndex);
            long index = @this.IndexOf(item, itemLength);
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
public class BitReader : IBitReader
{
    public static BitReader Empty { get; } = new BitReader(Array.Empty<ulong>(), 0);
    private readonly BitArray data;
    /// <summary>
    /// The index where this reader actually starts. Cannot be sought beyond.
    /// Treat this BitReader as if the stream really starts at <see cref="startOffset"/>.
    /// </summary>
    private readonly ulong startOffset;
    /// <summary> Gets the length of the stream this bitreader can read, in bits. </summary>
    public ulong Length { get; } // does not count the bits before the startOffset
    private ulong End => startOffset + Length;

    /// <summary> In bits. </summary>
    private ulong current;
    /// <summary> In bits. </summary>
    public ulong RemainingLength => this.End - current;
    /// <summary>
    /// Gets the current position of this reader in the bit stream.
    /// </summary>
    public ulong Position => current - startOffset;

    /// <summary>
    /// Gets the remainder of the bits in a segment.
    /// </summary>
    public BitArrayReadOnlySegment RemainingSegment
    {
        get => this.data[checked((int)this.current)..];
    }
    [DebuggerHidden]
    public BitReader(BitArray data, ulong startBitIndex = 0)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (startBitIndex > data.Length) throw new ArgumentOutOfRangeException(nameof(startBitIndex));

        this.data = data;
        this.startOffset = startBitIndex;
        this.current = startOffset;
        this.Length = data.Length - startBitIndex;
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

    public ulong ReadUInt64(int bitCount = 64)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if ((ulong)bitCount > this.RemainingLength)
            throw new ArgumentException(nameof(bitCount));

        ulong bits = this.data.GetULong(this.current);
        ulong mask = BitArrayExtensions.LowerBitsMask(bitCount);
        ulong result = bits & mask;

        current += (ulong)bitCount;

        return result;
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
    /// Gets all indices in the stream the pattern occurs at.
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        return IBitReader.IndicesOfImpl(this, item, itemLength, startBitIndex);
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
    private string ToDebuggerDisplay()
    {
        return $"BitReader({startOffset}..[|{current}|]..{End}, Length={this.Length}/{this.data.Length}, Remaining={this.RemainingLength})";
    }

    public IBitReader Clone()
    {
        var result = new BitReader(this.data, this.startOffset, this.Length);
        result.current = this.current;
        return result;
    }
    /// <param name="range">Relative to the complete bit array, not relative to the remaining part.</param>
    public IBitReader this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)this.Length));
            return new BitReader(this.data, this.startOffset + (ulong)offset, (ulong)length);
        }
    }
}

class InsufficientBitsException : ArgumentOutOfRangeException
{
    public InsufficientBitsException() : base($"Insufficient bits remaining in stream") { }
    public InsufficientBitsException(string elementName) : base($"Insufficient bits remaining in stream to read '{elementName}'") { }
}