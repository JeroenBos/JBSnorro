#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DebuggerDisplayAttribute = System.Diagnostics.DebuggerDisplayAttribute;
namespace JBSnorro.Collections.Bits.Internals;

[DebuggerDisplay("{ToDebuggerDisplay()}")]
internal abstract class BitReader : IBitReader
{
    protected readonly BitArray data;
    /// <summary>
    /// The index where this reader actually starts. Cannot be sought beyond.
    /// Treat this BitReader as if the stream really starts at <see cref="startOffset"/>.
    /// </summary>
    protected readonly ulong startOffset;
    /// <summary> In bits, relative to the start of <see cref="data"/>. </summary>
    protected ulong current;
    /// <summary> 
    /// Gets the length of the stream this <see cref="IBitReader"/> can read, in bits.
    /// </summary>
    /// <remarks>Does not count the bits before the <see cref="startOffset"/>.</remarks>
    public ulong Length { get; }

    private ulong End
    {
        get => startOffset + Length;
    }
    /// <summary> In bits.</summary>
    public ulong RemainingLength
    {
        get => End - current;
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
        get => data[checked((int)current)..];
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
        startOffset = startBitIndex;
        current = startBitIndex;
        Length = length;
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
        current = startBitIndex;
        Length = dataBitCount - startBitIndex;
    }
    [DebuggerHidden]
    public BitReader(BitArrayReadOnlySegment data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        this.data = data.data;
        startOffset = data.start;
        current = startOffset;
        Length = data.Length;
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
        if ((ulong)bitCount > RemainingLength)
            throw new ArgumentException("Not enough remaining length", nameof(bitCount));

        ulong bits = data.GetULong(current);
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
        Contract.Requires<ArgumentOutOfRangeException>(bitIndex <= Length);

        current = bitIndex;
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    /// <returns>-1 if not found.</returns>
    public long IndexOf(ulong item, int itemLength = 64)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);

        return data.IndexOf(item, itemLength, Position);
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
        if (startBitIndex < 0 || startBitIndex + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (destBitIndex < 0 || (ulong)destBitIndex + length > (ulong)dest.Length * 64UL)
            throw new ArgumentOutOfRangeException(nameof(destBitIndex));

        data.CopyTo(dest, destBitIndex);
    }
    public abstract IBitReader Clone();


    protected virtual string ToDebuggerDisplay()
    {
        return $"{GetType().Name}({startOffset}..[|{current}|]..{End}, Length={Length}/{data.Length}, Remaining={RemainingLength})";
    }

    /// <summary>
    /// It doesn't matter which derivative type you use, if you're only going to read UInt64s: the implementations of ReadUInt64 don't differ
    /// </summary>
    internal static ulong ReadUInt64(BitArray data, ulong startIndex, int dataLength)
    {
        return new SomeBitReader(data, startIndex).ReadUInt64(dataLength);
    }
}


internal class FloatingPointBitReader : IFloatingPointBitReader
{
    private readonly IBitReader reader;
    private readonly Func<int, IBitReader, double> readDouble;

    public FloatingPointBitReader(IBitReader reader, Func<int /*bitCount*/, IBitReader, double> readDouble)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.readDouble = readDouble ?? throw new ArgumentNullException(nameof(readDouble));
    }

    public IBitReader this[Range range]
    {
        get => reader[range];
    }
    public ulong Length
    {
        get => reader.Length;
    }
    public ulong Position
    {
        get => reader.Position;
    }


    public double ReadDouble(int bitCount = 64)
    {
        return readDouble(bitCount, reader);
    }


    public IBitReader Clone()
    {
        return reader.Clone();
    }
    public ulong ReadUInt64(int bitCount = 64)
    {
        return reader.ReadUInt64(bitCount);
    }
    public void Seek(ulong bitIndex)
    {
        reader.Seek(bitIndex);
    }
}