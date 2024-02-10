using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Collections.Bits;


public delegate IBitReader IBitReaderFactory(BitArray data, ulong startBitIndex = 0);
public interface IBitReader
{
    /// <summary>
    /// This is the function that creates <see cref="BitReader"/>s, and can be overridden.
    /// This is the only place where I make the decision to default to <see cref="SomeBitReader"/> of all the <see cref="IBitReader"/>.
    /// I.e. <see cref="BitArray.ToBitReader()"/> and <see cref="BitArrayReadOnlySegment.ToBitReader(ulong)"/> defer to this.
    /// 
    /// </summary>
    public static IBitReaderFactory Factory = (data, startBitIndex) => new BitReader(data, startBitIndex);
    public static IBitReader Create(BitArray data) => Factory.Invoke(data, 0);
    public static IBitReader Create(BitArray data, ulong startBitIndex) => Factory(data, startBitIndex);
    public static IBitReader Create(BitArray data, ulong startBitIndex, ulong length)
    {
        return new BitReader(data, startBitIndex, length);
    }


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
    long IndexOf(ulong item, int itemLength) => IndexOf(item, itemLength, Position);

    IBitReader Clone();
    /// <summary>
    /// Gets an <see cref="IBitReader"/> reading the next <paramref name="bitCount"/> bits of this reader.</summary>
    /// <param name="bitCount">The next number of bits the resulting <see cref="IBitReader"/> will have access to.</param>
    /// <param name="tagAlong">Whether the current bitreader should keep updating its position when the resulting <see cref="IBitReader"/> moves its position.</param>
    IBitReader this[ulong bitCount, bool tagAlong = true] { get; }


    public bool ReadBit()
    {
        if (RemainingLength < 1)
            throw new InsufficientBitsException("bit");

        ulong result = ReadUInt64(1);
        return result != 0;
    }
    [DebuggerHidden]
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("byte");

        return (byte)ReadUInt64(bitCount);
    }
    [DebuggerHidden]
    public sbyte ReadSByte(int bitCount = 8)
    {
        if (bitCount < 2 || bitCount > 8)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("sbyte");

        return (sbyte)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 16)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("short");

        return (short)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public ushort ReadUInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("ushort");

        return (ushort)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (int)ReadInt64(bitCount);
    }
    [DebuggerHidden]
    public uint ReadUInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("int");

        return (uint)ReadUInt64(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 2 || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("long");

        bool sign = ReadUInt64(1) == 0;
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
    ulong ReadUInt64(int bitCount = 64);

    /// <summary>
    /// Gets all indices in the stream the pattern occurs at.
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<long> IndicesOf(ulong item, int itemLength, ulong startBitIndex = 0)
    {
        Contract.Requires<ArgumentOutOfRangeException>(itemLength >= 1);
        Contract.Requires<ArgumentOutOfRangeException>(itemLength <= 64);
        Contract.Requires<ArgumentOutOfRangeException>(0 <= startBitIndex);
        Contract.Requires<ArgumentOutOfRangeException>(startBitIndex <= Length);

        var nextBitIndex = startBitIndex;
        while (true)
        {
            Seek(nextBitIndex);
            long index = IndexOf(item, itemLength);
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
