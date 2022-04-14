#nullable enable
using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
    public sealed class BitArrayReadOnlySegment : IReadOnlyList<bool>
    {
        internal protected readonly BitArray data;
        internal protected readonly ulong start;

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
            return new BitReader(data, this.start + startIndex, this.Length);
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

                ulong dataBeforeCurrentSegment = new BitReader(this.data, (ulong)dataStart).ReadUInt64(dataLength);
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

                ulong dataAfterCurrentSegment = new BitReader(this.data, this.start + this.Length).ReadUInt64(dataLength);
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
        public bool Equals(BitArrayReadOnlySegment other)
        {
            return this.data.BitSequenceEqual(other, this.start, this.Length);
        }
        public bool Equals(BitArray other)
        {
            return this.data.BitSequenceEqual(other, this.start, this.Length);
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
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
        public static BitArrayReadOnlySegment SelectSegment(this BitArray array, int start, int length)
        {
            return array.SelectSegment((ulong)start, (ulong)length);
        }
        public static BitArrayReadOnlySegment SelectSegment(this BitArray array, ulong start, ulong length)
        {
            return new BitArrayReadOnlySegment(array, start, length);
        }
    }
}
