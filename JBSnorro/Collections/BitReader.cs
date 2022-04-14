#nullable enable
using System.Diagnostics;

namespace JBSnorro.Collections;

public interface IBitReader
{
    ulong Length { get; }
    bool ReadBit();
    byte ReadByte(int bitCount);
    short ReadInt16(int bitCount);
    int ReadInt32(int bitCount);
    long ReadInt64(int bitCount);
    ulong ReadUInt64(int bitCount);

    Half ReadHalf();
    float ReadSingle();
    double ReadDouble();
    void Seek(ulong bitIndex);
}
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public class BitReader : IBitReader
{
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
    private int ulongIndex => checked((int)(current / 64));
    private int bitIndex => checked((int)(current % 64));
    /// <summary> In bits. </summary>
    public ulong RemainingLength => this.End - current;
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
    public Half ReadHalf()
    {
        if (this.RemainingLength < 16)
            throw InsufficientBitsException("Half");
        short i = ReadInt16();
        return BitTwiddling.BitsAsHalf(i);
    }
    public float ReadSingle()
    {
        if (this.RemainingLength < 32)
            throw InsufficientBitsException("Single");
        int i = ReadInt32();
        return BitTwiddling.BitsAsSingle(i);
    }
    public double ReadDouble()
    {
        if (this.RemainingLength < 64)
            throw InsufficientBitsException("Double");
        long i = unchecked((long)ReadUInt64(64));
        return BitTwiddling.BitsAsDouble(i);
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
        return RemainingLength > (ulong)bitCount;
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
        if (bitIndex < 0 || bitIndex > this.Length)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        this.current = bitIndex;
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    public long Find(ulong pattern, int patternLength)
    {
        if (patternLength < 1)
            throw new ArgumentOutOfRangeException(nameof(patternLength));
        if (patternLength > 64)
            throw new NotSupportedException("patternLength > 64");

        checked
        {
            ulong mask = BitArrayExtensions.LowerBitsMask(patternLength);

            for (; this.current + (ulong)patternLength < this.Length; this.current -= ((ulong)patternLength - 1))
            {
                ulong value = this.ReadUInt64(patternLength);
                if (((value ^ pattern) & mask) == mask)
                {
                    long currentIndex = (long)this.current;
                    return currentIndex - patternLength;
                }
            }
            this.current = this.Length;
            return -1;
        }
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    public int Find(ulong pattern, int patternLength, ulong startBitIndex)
    {
        if (startBitIndex < 0 || startBitIndex > this.Length)
            throw new ArgumentOutOfRangeException(nameof(startBitIndex));
        if (patternLength < 1)
            throw new ArgumentOutOfRangeException(nameof(patternLength));
        if (patternLength > 64)
            throw new ArgumentOutOfRangeException(nameof(patternLength), "> 64");

        this.Seek(startBitIndex);
        return this.Find(pattern, patternLength, startBitIndex);
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
}
