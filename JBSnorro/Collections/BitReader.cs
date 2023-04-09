#nullable enable
using JBSnorro.Diagnostics;
using DebuggerDisplayAttribute = System.Diagnostics.DebuggerDisplayAttribute;
namespace JBSnorro.Collections;

public interface IBitReader
{
    ulong Length { get; }
    ulong RemainingLength { get; }
    ulong Position { get; }

    bool ReadBit();
    byte ReadByte(int bitCount = 8);
    short ReadInt16(int bitCount = 16);
    ushort ReadUInt16(int bitCount = 16);
    int ReadInt32(int bitCount = 32);
    uint ReadUInt32(int bitCount = 32);
    long ReadInt64(int bitCount = 64);
    ulong ReadUInt64(int bitCount = 64);
    Half ReadHalf(int bitCount = 16);
    float ReadSingle(int bitCount = 32);
    double ReadDouble(int bitCount = 64);
    double ReadVariableFloatingPoint(int bitCount);


    bool CanRead(int bitCount)
    {
        if (bitCount < 0) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return CanRead((ulong)bitCount);
    }
    bool CanRead(ulong bitCount);
    void Seek(ulong bitIndex);
    long IndexOf(ulong item, int itemLength);
    
    IBitReader Clone();
    BitArrayReadOnlySegment RemainingSegment { get; }
    BitArrayReadOnlySegment this[Range range] { get; }
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
    public BitReader(BitArray data, ulong startBitIndex = 0)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (startBitIndex > data.Length) throw new ArgumentOutOfRangeException(nameof(startBitIndex));

        this.data = data;
        this.startOffset = startBitIndex;
        this.current = startOffset;
        this.Length = data.Length - startBitIndex;
    }
    public BitReader(BitArray data, ulong startBitIndex, ulong length)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (startBitIndex + length > data.Length) throw new ArgumentOutOfRangeException(nameof(startBitIndex));

        this.data = data;
        this.startOffset = startBitIndex;
        this.current = startBitIndex;
        this.Length = length;
    }
    /// <param name="dataBitCount"> The length of the number of bits in <see cref="data"/>, including those to be excluded before <see cref="startBitIndex"/></param>
    public BitReader(ulong[] data, int dataBitCount, int startBitIndex = 0)
        : this(data, dataBitCount.ToULong(), startBitIndex.ToULong())
    {
    }
    /// <param name="dataBitCount"> The length of the number of bits in <see cref="data"/>, including those to be excluded before <see cref="startBitIndex"/></param>
    public BitReader(ulong[] data, ulong dataBitCount, ulong startBitIndex = 0)
    {
        this.data = BitArray.FromRef(data, dataBitCount);
        this.current = startBitIndex;
        this.Length = dataBitCount - startBitIndex;
    }

    private static Exception InsufficientBitsException(string elementName)
    {
        return new InvalidOperationException($"Insufficient bits remaining in stream to read '{elementName}'");
    }
    public bool ReadBit()
    {
        if (this.RemainingLength < 1)
            throw InsufficientBitsException("bit");

        bool result = this.data[this.current];
        current++;
        return result;
    }
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("byte");

        return (byte)ReadUInt64(bitCount);
    }
    public sbyte ReadSByte(int bitCount = 8)
    {
        if (bitCount < 2 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("sbyte");

        return (sbyte)ReadInt64(bitCount);
    }
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("short");

        return (short)ReadInt64(bitCount);
    }
    public ushort ReadUInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("ushort");

        return (ushort)ReadInt64(bitCount);
    }
    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("int");

        return (int)ReadInt64(bitCount);
    }
    public uint ReadUInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("int");

        return (uint)ReadUInt64(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("long");

        bool sign = ReadBit();
        var magnitude = (long)ReadUInt64(bitCount - 1);
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
            throw InsufficientBitsException("Half");

        // half has 5 bits exponent
        return (Half)readDouble(bitCount);
    }
    public float ReadSingle(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("float");

        // float has 8 bits exponent
        return (float)readDouble(bitCount);
    }
    public double ReadDouble(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < (ulong)bitCount)
            throw InsufficientBitsException("double");

        // double has 11 bits exponent
        return readDouble(bitCount);
    }
    /// <summary>
    /// First reads a number of how many bits to read, and then reads that number of bits as a float point number.
    /// </summary>
    public double ReadVariableFloatingPoint(int maxBitCount = 32)
    {
        if (maxBitCount < 2 || maxBitCount > 64)
            throw new ArgumentException(nameof(maxBitCount));
        if (RemainingLength < 3)
            throw InsufficientBitsException("variable floating point");

        if (RemainingLength < 7)
            return this.readDouble((int)RemainingLength);

        int bitsToRead = (int)this.ReadUInt32(5);
        int clampedBitsToRead = Math.Max(2, Math.Min(maxBitCount, bitsToRead));
        if ((ulong)clampedBitsToRead > RemainingLength)
        {
            clampedBitsToRead = (int)RemainingLength;
        }
        return readDouble(clampedBitsToRead);
    }
    private double readDouble(int bitCount)
    {
        // the idea is that if the mantissa is filled, then the bits are just becoming less and less relevant
        // but before some bitCount, there simply isn't enough bits to have this strategy. Something more complicated (or simpler) is needed

        int significantBitCount = Math.Max(2, bitCount / 2);
        int exponentBitCount = bitCount - significantBitCount;

        var significant = ReadInt64(significantBitCount);
        var exponent = exponentBitCount == 0 ? 1 : exponentBitCount == 1 ? (double)ReadUInt64(exponentBitCount) : (double)ReadInt64(exponentBitCount);

        var value = 2 * significant * double.Pow(2, exponent);
        return value;
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
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    /// <returns>-1 if not found.</returns>
    public long IndexOf(ulong item, int itemLength, ulong startBitIndex)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= this.Length);
        Contract.Requires<NotImplementedException>(itemLength <= 64);

        this.Seek(startBitIndex);
        return this.IndexOf(item, itemLength);
    }
    /// <summary>
    /// Gets all indices in the stream the pattern occurs at.
    /// </summary>
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= this.Length);

        ulong nextBitIndex = startBitIndex;
        while (true)
        {
            long index = this.IndexOf(item, itemLength, nextBitIndex);
            if (index == -1)
                yield break;
            yield return index;
            nextBitIndex = checked((ulong)index + (ulong)itemLength);
        }
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
    // mostly you want RemainingSegment anyway...
    // public BitArrayReadOnlySegment ToBitArraySegment()
    // {
    //     checked
    //     {
    //         return this.data[new Range((int)this.startOffset, (int)this.startOffset + (int)this.Length)];
    //     }
    // }

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
    public BitArrayReadOnlySegment this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(checked((int)this.Length));
            return this.data[new Range((int)this.startOffset + offset, length)];
        }
    }

}
