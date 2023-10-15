using JBSnorro;
using JBSnorro.Algorithms;
using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;

namespace JBSnorro.Collections.Bits;

[DebuggerDisplay("BitArrayReadOnlySegment(Length={Length}, {this.ToString()})")]
public sealed class BitArrayReadOnlySegment : IReadOnlyList<bool>
{
    public static BitArrayReadOnlySegment Empty { get; } = new BitArrayReadOnlySegment(new BitArray(Array.Empty<ulong>(), 0), 0, 0);
    internal readonly BitArray data;
    internal readonly ulong start;

    public BitArrayReadOnlySegment(BitArray data, ulong start, ulong length)
    {
        this.data = data;
        this.start = start;
        Length = length;
    }

    public bool this[int index] => this[(ulong)index];
    public bool this[ulong index] => data[start + index];
    public BitArrayReadOnlySegment this[ulong index, ulong exclusiveEnd]
    {
        get
        {
            checked
            {
                return this[new Range((int)index, (int)exclusiveEnd)];
            };
        }
    }
    public BitArrayReadOnlySegment this[Range range]
    {
        get
        {
            checked
            {
                Contract.Requires<NotImplementedException>(Length <= int.MaxValue);
                var (start, length) = range.GetOffsetAndLength((int)Length);
                ulong dataStart = (uint)start + this.start;
                Contract.Requires<NotImplementedException>(dataStart <= int.MaxValue);

                var dataRange = new Range((int)dataStart, (int)dataStart + length);
                return data[dataRange];
            }
        }
    }

    public ulong Length { get; private set; }

    public IEnumerator<bool> GetEnumerator()
    {
        Contract.Assert<NotImplementedException>(start + Length <= int.MaxValue);
        return Enumerable.Range(0, ((IReadOnlyList<bool>)this).Count).Select(i => this[i]).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public IBitReader ToBitReader(ulong startIndex = 0)
    {
        return IBitReader.Create(data, start + startIndex, Length);
    }
    int IReadOnlyCollection<bool>.Count
    {
        [DebuggerHidden] get
        {
            Contract.Assert<NotImplementedException>(Length <= int.MaxValue);
            return (int)Length;
        }

    }
    public long IndexOf(ulong item, int? itemLength = null, ulong startBitIndex = 0)
    {
        return data.IndexOf(item, itemLength, startBitIndex);
    }
    public (long BitIndex, int ItemIndex) IndexOfAny(ulong[] items, int? itemLength = null, ulong startIndex = 0)
    {
        return data.IndexOfAny(items, itemLength, start + startIndex, endIndex: start + Length);
    }
    public void CopyTo(BitArray dest, ulong destStartIndex)
    {
        CopyTo(dest, 0, Length, destStartIndex);
    }
    public void CopyTo(BitArray dest, ulong sourceStartIndex, ulong length, ulong destStartIndex)
    {
        if (length > Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (sourceStartIndex + length > start + Length) throw new ArgumentOutOfRangeException(nameof(sourceStartIndex));

        data.CopyTo(dest, start + sourceStartIndex, length, destStartIndex);
    }

    public BitArray Insert(ulong data, int dataLength, ulong insertionIndex)
    {
        var result = new BitArray(Length + (ulong)dataLength);
        CopyTo(result, 0UL, insertionIndex, 0);
        result.Set(data, dataLength, insertionIndex);
        CopyTo(result, insertionIndex, Length - insertionIndex, (ulong)dataLength);
        return result;
    }
    public BitArrayReadOnlySegment Prepend(ulong data, int dataLength)
    {
        if (dataLength < 0 || dataLength > 64) throw new ArgumentOutOfRangeException(nameof(dataLength));

        if (isPrependedWithData())
        {
            // this case is a performance optimization
            return new BitArrayReadOnlySegment(this.data, start - (ulong)dataLength, Length + (ulong)dataLength);
        }
        else
        {
            return this.data.Prepend(data, dataLength).SelectSegment(Range.All);
        }

        bool isPrependedWithData()
        {
            long dataStart = (long)start - dataLength;
            if (dataStart < 0)
                return false;

            // SomeBitReader or some other type doesn't matter: the implementation of ReadUInt64 doesn't differ
            ulong dataBeforeCurrentSegment = BitReader.ReadUInt64(this.data, (ulong)dataStart, dataLength);
            return dataBeforeCurrentSegment == data;
        }
    }
    public BitArrayReadOnlySegment Append(ulong data, int dataLength)
    {
        if (dataLength < 0 || dataLength > 64) throw new ArgumentOutOfRangeException(nameof(dataLength));

        if (isAppendedWithData())
        {
            // this case is a performance optimization
            return new BitArrayReadOnlySegment(this.data, start, Length + (ulong)dataLength);
        }
        else
        {
            return this.data.Prepend(data, dataLength).SelectSegment(Range.All);
        }

        bool isAppendedWithData()
        {
            ulong dataEnd = start + Length + (ulong)dataLength;
            if (dataEnd > this.data.Length)
                return false;


            ulong dataAfterCurrentSegment = BitReader.ReadUInt64(this.data, start + Length, dataLength);
            return dataAfterCurrentSegment == data;
        }
    }
    /// <summary>
    /// Prepends and appends data to this readonly segment.
    /// Convenience method and perf optimization for prepending and appending data.
    /// </summary>
    public BitArrayReadOnlySegment Wrap(ulong prependData, int prependDataLength, ulong appendData, int appendDataLength)
    {
        return Prepend(prependData, prependDataLength)
                   .Append(appendData, appendDataLength);
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            BitArrayReadOnlySegment segment => Equals(segment),
            BitArray array => Equals(array),
            _ => false
        };
    }
    public bool Equals(ulong other)
    {
        if (Length > 64)
            return false;
        return Equals(new BitArray(new ulong[] { other }, Length));
    }
    public bool Equals(BitArrayReadOnlySegment other)
    {
        return data.BitSequenceEqual(other, start, Length);
    }
    public bool Equals(BitArray other)
    {
        return data.BitSequenceEqual(other[Range.All], start, Length);
    }
    public override int GetHashCode()
    {
        ComputeSHA1(out ISHAThatCanContinue hasher);
        return hasher.GetHashCode();
    }

    public string ComputeSHA1()
    {
        using var hasher = ISHAThatCanContinue.Create();
        ComputeSHA1(hasher);
        return hasher.ToString();
    }
    public void ComputeSHA1(out ISHAThatCanContinue hasher)
    {
        ComputeSHA1(hasher = ISHAThatCanContinue.Create());
    }
    public void ComputeSHA1(ISHAThatCanContinue hasher)
    {
        // PERF
        var copy = new BitArray(Length);
        CopyTo(copy, 0);

        copy.ComputeSHA1(hasher);
    }
    public override string ToString()
    {
        return data.ToString(start, Length);
    }
    public ReadOnlySpan<byte> GetUnderlyingData(out ulong start, bool minimize = true)
    {
        if (!minimize)
        {
            start = this.start;
            return data.UnderlyingData;
        }
        else
        {
            var startBoundaryBitIndex = this.start.RoundDownToNearestMultipleOf(64UL);
            var endBoundaryBitIndex = (this.start + Length).RoundUpToNearestMultipleOf(64UL);
            var startBoundaryByteIndex = startBoundaryBitIndex / 8;
            var endBoundaryByteIndex = endBoundaryBitIndex / 8;

            start = this.start - startBoundaryBitIndex;
            var result = data.UnderlyingData[checked((int)startBoundaryByteIndex..(int)endBoundaryByteIndex)];
            return result;
        }
    }

    /// <summary>
    /// Counts the number of ones in this bitarray.
    /// </summary>
    public ulong CountOnes()
    {
        ulong result = 0;
        foreach (bool bit in this)
        {
            if (bit)
            {
                result++;
            }
        }
        return result;
    }
    public BitArrayReadOnlySegment Or(BitArrayReadOnlySegment other)
    {
        Contract.Requires(other != null);
        Contract.Requires(other.Length == this.Length);

        var cloned = new BitArray(this);
        cloned.Or(other);
        return new BitArrayReadOnlySegment(cloned, 0, cloned.Length);
    }
    public BitArrayReadOnlySegment Xor(BitArrayReadOnlySegment other)
    {
        Contract.Requires(other != null);
        Contract.Requires(other.Length == this.Length);

        var cloned = new BitArray(this);
        cloned.Xor(other);
        return new BitArrayReadOnlySegment(cloned, 0, cloned.Length);
    }
    public BitArrayReadOnlySegment And(BitArrayReadOnlySegment other)
    {
        Contract.Requires(other != null);
        Contract.Requires(other.Length == this.Length);

        var cloned = new BitArray(this);
        cloned.And(other);
        return new BitArrayReadOnlySegment(cloned, 0, cloned.Length);
    }
}

public static class BitArraySegmentExtensions
{
    [DebuggerHidden]
    public static BitArrayReadOnlySegment SelectSegment(this BitArray array, Range range)
    {
        Contract.Assert<NotImplementedException>(array.Length <= int.MaxValue);
        var (index, length) = range.GetOffsetAndLength((int)array.Length);
        return array.SelectSegment(index, length);
    }
    [DebuggerHidden]
    public static BitArrayReadOnlySegment SelectSegment(this BitArray array, int start, int length)
    {
        return array.SelectSegment((ulong)start, (ulong)length);
    }
    [DebuggerHidden]
    public static BitArrayReadOnlySegment SelectSegment(this BitArray array, ulong start, ulong length)
    {
        return new BitArrayReadOnlySegment(array, start, length);
    }
}
