#nullable enable
using JBSnorro.Algorithms;
using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;

namespace JBSnorro.Collections
{
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
            this.Length = length;
        }

        public bool this[int index] => this[(ulong)index];
        public bool this[ulong index] => this.data[start + index];
        public BitArrayReadOnlySegment this[Range range]
        {
            get
            {
                checked
                {
                    Contract.Requires<NotImplementedException>(this.Length <= int.MaxValue);
                    var (start, length) = range.GetOffsetAndLength((int)this.Length);
                    ulong dataStart = (uint)start + this.start;
                    Contract.Requires<NotImplementedException>(dataStart <= int.MaxValue);

                    var dataRange = new Range((int)dataStart, (int)dataStart + length);
                    return this.data[dataRange];
                }
            }
        }

        public ulong Length { get; private set; }

        public IEnumerator<bool> GetEnumerator()
        {
            Contract.Assert<NotImplementedException>(start + this.Length <= int.MaxValue);
            return Enumerable.Range(0, ((IReadOnlyList<bool>)this).Count).Select(i => this[i]).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public BitReader ToBitReader(ulong startIndex = 0)
        {
            return BitReader.Create(data, this.start + startIndex, this.Length);
        }
        int IReadOnlyCollection<bool>.Count
        {
            get
            {
                Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
                return (int)this.Length;
            }

        }
        public long IndexOf(ulong item, int? itemLength = null, ulong startBitIndex = 0)
        {
            return this.data.IndexOf(item, itemLength, startBitIndex);
        }
        public (long BitIndex, int ItemIndex) IndexOfAny(IReadOnlyList<ulong> items, int? itemLength = null, ulong startIndex = 0)
        {
            return this.data.IndexOfAny(items, itemLength, this.start + startIndex, endIndex: this.start + this.Length);
        }
        public void CopyTo(BitArray dest, ulong destStartIndex)
        {
            this.CopyTo(dest, 0, this.Length, destStartIndex);
        }
        public void CopyTo(BitArray dest, ulong sourceStartIndex, ulong length, ulong destStartIndex)
        {
            if (length > this.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (sourceStartIndex + length > this.start + this.Length) throw new ArgumentOutOfRangeException(nameof(sourceStartIndex));

            this.data.CopyTo(dest, this.start + sourceStartIndex, length, destStartIndex);
        }

        public BitArray Insert(ulong data, int dataLength, ulong insertionIndex)
        {
            var result = new BitArray(this.Length + (ulong)dataLength);
            this.CopyTo(result, 0UL, insertionIndex, 0);
            result.Set(data, dataLength, insertionIndex);
            this.CopyTo(result, insertionIndex, this.Length - insertionIndex, (ulong)dataLength);
            return result;
        }
        public BitArrayReadOnlySegment Prepend(ulong data, int dataLength)
        {
            if (dataLength < 0 || dataLength > 64) throw new ArgumentOutOfRangeException(nameof(dataLength));

            if (isPrependedWithData())
            {
                // this case is a performance optimization
                return new BitArrayReadOnlySegment(this.data, this.start - (ulong)dataLength, this.Length + (ulong)dataLength);
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
                return new BitArrayReadOnlySegment(this.data, this.start, this.Length + (ulong)dataLength);
            }
            else
            {
                return this.data.Prepend(data, dataLength).SelectSegment(Range.All);
            }

            bool isAppendedWithData()
            {
                ulong dataEnd = start + this.Length + (ulong)dataLength;
                if (dataEnd > this.data.Length)
                    return false;

                
                ulong dataAfterCurrentSegment = BitReader.ReadUInt64(this.data, this.start + this.Length, dataLength);
                return dataAfterCurrentSegment == data;
            }
        }
        /// <summary>
        /// Prepends and appends data to this readonly segment.
        /// Convenience method and perf optimization for prepending and appending data.
        /// </summary>
        public BitArrayReadOnlySegment Wrap(ulong prependData, int prependDataLength, ulong appendData, int appendDataLength)
        {
            return this.Prepend(prependData, prependDataLength)
                       .Append(appendData, appendDataLength);
        }

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                BitArrayReadOnlySegment segment => this.Equals(segment),
                BitArray array => this.Equals(array),
                _ => false
            };
        }
        public bool Equals(ulong other)
        {
            if (this.Length > 64)
                return false;
            return this.Equals(new BitArray(new ulong[] { other }, this.Length));
        }
        public bool Equals(BitArrayReadOnlySegment other)
        {
            return this.data.BitSequenceEqual(other, this.start, this.Length);
        }
        public bool Equals(BitArray other)
        {
            return this.data.BitSequenceEqual(other[Range.All], this.start, this.Length);
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
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
            var copy = new BitArray(this.Length);
            this.CopyTo(copy, 0);

            copy.ComputeSHA1(hasher);
        }
        public override string ToString()
        {
            return this.data.ToString(this.start, this.Length);
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
                var endBoundaryBitIndex = (this.start + this.Length).RoundUpToNearestMultipleOf(64UL);
                var startBoundaryByteIndex = startBoundaryBitIndex / 8;
                var endBoundaryByteIndex = endBoundaryBitIndex / 8;

                start = this.start - startBoundaryBitIndex;
                var result = data.UnderlyingData[checked((int)startBoundaryByteIndex..(int)endBoundaryByteIndex)];
                return result;
            }
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
}
