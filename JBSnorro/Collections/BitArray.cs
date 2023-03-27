#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using static JBSnorro.Global;

namespace JBSnorro.Collections
{
    /// <summary> Represents the same idea as the BCL <see cref="System.Collections.BitArray"/>, 
    /// only it derives from <see cref="System.Collections.Generic.IList{T}"/> and <see cref="System.Collections.Generic.IReadOnlyList{T}"/>. </summary>
    [DebuggerDisplay("BitArray(Length={Length})")]
    public sealed class BitArray : IList<bool>, IReadOnlyList<bool>
    {
        public static BitArray Empty { get; } = new BitArray(Array.Empty<bool>());
        private const int bitCountPerInternalElement = 64;
        private static void ToInternalAndBitIndex(int index, out int dataIndex, out int bitIndex)
        {
            dataIndex = ToInternalAndBitIndex(index, out bitIndex);
        }
        private static int ToInternalAndBitIndex(int index, out int bitIndex)
        {
            return Math.DivRem(index, bitCountPerInternalElement, out bitIndex);
        }
        /// <summary> Gets the length of the internal data structure given the number of bits it should hold. </summary>
        /// <param name="bitCount"> The number of bits to store in the internal data. </param>
        [DebuggerHidden]
        internal static int ComputeInternalStructureSize(ulong bitCount)
        {
            var (division, remainder) = Math.DivRem(bitCount, bitCountPerInternalElement);
            checked
            {
                return (int)(division + (remainder == 0 ? 0UL : 1UL));
            }
        }
        /// <summary>
        /// Creates a new bit array of the specified length populated with random data.
        /// </summary>
        /// <param name="length">The number of bits this array is to represent.</param>
        public static BitArray InitializeRandom(ulong length, Random random)
        {
            var data = new ulong[ComputeInternalStructureSize(length)];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = random.NextUInt64();
            }
            // we _could_ clear out the bits after `length` but I don't think it's necessary atm
            return new BitArray(data, length);
        }

        private ulong[] data;
        /// <summary> Gets or sets the flag at the specified index in this array is set. </summary>
        public bool this[int index]
        {
            [DebuggerHidden]
            get => this[(ulong)index];
            [DebuggerHidden]
            set => this[(ulong)index] = value;
        }
        /// <summary> Gets or sets the flag at the specified index in this array is set. </summary>
        public bool this[ulong index]
        {
            [DebuggerHidden]
            get
            {
                Contract.Requires<IndexOutOfRangeException>(0 <= index);
                Contract.Requires<NotImplementedException>(index <= int.MaxValue);
                int i = (int)index;
                Contract.Requires<IndexOutOfRangeException>(index < Length);

                int bitIndex;
#if DEBUG
                int dataIndex;
                ToInternalAndBitIndex(i, out dataIndex, out bitIndex);
#endif
                return (data[ToInternalAndBitIndex(i, out bitIndex)] & (1UL << bitIndex)) != 0;
            }
            [DebuggerHidden]
            set
            {
                Contract.Requires<IndexOutOfRangeException>(0 <= index);
                Contract.Requires<NotImplementedException>(index <= int.MaxValue);
                int i = (int)index;
                Contract.Requires<IndexOutOfRangeException>(index < Length);

                if (value)
                {
                    int dataIndex, bitIndex;
                    ToInternalAndBitIndex(i, out dataIndex, out bitIndex);
                    data[dataIndex] |= 1UL << bitIndex;
                }
            }
        }

        public BitArrayReadOnlySegment this[Range range]
        {
            get
            {
                Contract.Requires<NotImplementedException>(this.Length <= int.MaxValue);
                var (start, length) = range.GetOffsetAndLength((int)this.Length);
                return new BitArrayReadOnlySegment(this, (ulong)start, (ulong)length);
            }
        }

        /// <summary>
        /// Gets the 64 successive bits at the specified bit index, padded with zeroes if necessary.
        /// </summary>
        internal ulong GetULong(ulong bitIndex)
        {
            if (bitIndex >= this.Length)
                throw new ArgumentOutOfRangeException(nameof(bitIndex));

            checked
            {
                int ulongIndex = (int)(bitIndex / 64);
                int bitIndexInUlong = (int)(bitIndex % 64);

                if (bitIndexInUlong == 0)
                {
                    return this.data[ulongIndex];
                }
                else
                {
                    ulong shiftedP1 = this.data[ulongIndex] >> bitIndexInUlong;
                    ulong shiftedP2 = this.data.ElementAtOrDefault(ulongIndex + 1) << (64 - bitIndexInUlong);

                    var result = shiftedP1 | shiftedP2;
                    return result;
                }
            }
        }

        /// <summary> Gets the number of bits in this bit array. </summary>
        public ulong Length { get; private set; }

        /// <summary>
        /// Uses the specified array directly as underlying data source.
        /// </summary>
        /// <param name="length">The number of bits in <paramref name="data"/>. Defaults to <code>64 * data.Length</code>.</param>
        public static BitArray FromRef(ulong[] data, ulong? length = null)
        {
            length ??= 64 * (ulong)data.Length;
            Contract.Assert<NotImplementedException>(length <= int.MaxValue);

            return new BitArray(data, (int)length.Value);
        }
        /// <summary> Creates a new empty bit array. </summary>
        public BitArray()
        {
            data = EmptyCollection<ulong>.Array;
        }
        /// <summary> Creates a new empty bit array containing the specified number of the specified value. </summary>
        [DebuggerHidden]
        public BitArray(int length, bool defaultValue = false) : this(length.ToULong(), defaultValue)
        {
        }
        /// <summary> Creates a new empty bit array containing the specified number of the specified value. </summary>
        [DebuggerHidden]
        public BitArray(ulong length, bool defaultValue = false)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            data = new ulong[ComputeInternalStructureSize(length)];
            Length = length;
            if (defaultValue)
            {
                for (ulong i = 0; i < length; i++)
                {
                    this[i] = true;
                }
            }
        }
        /// <summary> Creates a new bit array from the specified bits. </summary>
        public BitArray(IEnumerable<bool> bits) : this((bits as IList<bool>) ?? bits.ToList()) { }
        /// <summary> Creates a new bit array from the specified bits. </summary>
        [DebuggerHidden]
        public BitArray(IList<bool> bits) : this(bits, bits.Count) { }
        /// <summary> Creates a new bit array from the specified bits. </summary>
        [DebuggerHidden]
        public BitArray(IReadOnlyList<bool> bits) : this(bits, bits.Count) { }
        [DebuggerHidden]
        public BitArray(bool[] bits) : this(bits, bits.Length) { }
        /// <summary> Creates a new bit array by OR-ing the specified bit sequences together. </summary>
        /// <param name="bitsToOr"> Specify which bits are to be set. Are OR-ed together, so must have equal sizes. </param>
        public BitArray(IEnumerable<IReadOnlyList<bool>> bitsToOr) : this((IList<bool>)bitsToOr.Or()) { }
        [DebuggerHidden]
        private BitArray(IEnumerable<bool> bits, int count) : this(bits, count.ToULong())
        {

        }
        /// <summary> Creates a new bit array from the specified bits with prespecified length. </summary>
        [DebuggerHidden]
        private BitArray(IEnumerable<bool> bits, ulong count)
        {
            Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
            Contract.Requires(bits != null);
            Contract.Requires(count >= 0);
            Contract.LazilyAssertCount(ref bits, (int)count);

            this.Length = count;
            this.data = new ulong[ComputeInternalStructureSize(count)];
            int i = 0;
            foreach (bool bit in bits)
            {
                this[i++] = bit;
            }
        }
        /// <summary> Creates a new bit array where the indices of the initially true bits are specified. </summary>
        /// <param name="indicesOfTrueBits"> The indices of bits to set to true. </param>
        /// <param name="length"> The length of the bit array to create. </param>
        public BitArray(IEnumerable<int> indicesOfTrueBits, int length) : this(length)
        {
            Contract.Requires(indicesOfTrueBits != null);

            foreach (int index in indicesOfTrueBits)
                this[index] = true;
        }
        /// <summary> Creates a new bit array where the indices of the initially true bits are specified. </summary>
        /// <param name="indicesOfTrueBits"> The indices of bits to set to true. </param>
        /// <param name="length"> The length of the bit array to create. </param>
        public BitArray(int length, params int[] indicesOfTrueBits) : this(length)
        {
            Contract.Requires(indicesOfTrueBits != null);

            foreach (int index in indicesOfTrueBits)
                this[index] = true;
        }
        /// <summary> Creates a new bit array from the specified bytes (i.e. concat all bytes, each representing 8 bits). </summary>
        public BitArray(IEnumerable<byte> bytes) : this(bytes.SelectMany(b => ToBits(b)))
        {
        }
        /// <summary> Creates a new bit array from the specified bytes (i.e. concat all bytes, each representing 8 bits), with prescribed length. </summary>
        public BitArray(IEnumerable<byte> bytes, int bitCount) : this(bytes.SelectMany(b => ToBits(b)), count: bitCount)
        {
        }
        private static IEnumerable<bool> ToBits(byte b)
        {
            return Enumerable.Range(0, 8).Select(i => b.HasBit(i));
        }
        /// <inheritdoc cref="BitArray(ulong[], ulong)"/>
        public BitArray(ulong[] backingData, int length) : this(backingData, checked((ulong)length))
        { }
        /// <summary> Creates a new bit array from <param ref="backingData"/>, i.e. no copy is made. </summary>
        /// <param name="backingData"> The indices of bits to set to true. </param>
        /// <param name="length"> The number of bits that are considered to be set in the given data. </param>
        public BitArray(ulong[] backingData, ulong length)
        {
            Contract.Requires(backingData != null);
            Contract.Requires(0 <= length && length <= 64UL * (ulong)backingData.Length);

            this.Length = length;
            this.data = backingData;
        }

        /// <summary> Gets a clone of this bit array. </summary>
        [DebuggerHidden]
        public BitArray Clone()
        {
            var data = (ulong[])this.data.Clone();
            return BitArray.FromRef(data, (ulong)this.Length);
        }

        /// <summary> Returns whether no bit is set in both this and the specified array. </summary>
        public bool IsDisjointFrom(BitArray array)
        {
            Contract.Requires(array != null);
            Contract.Requires(this.Length == array.Length);

            for (int i = 0; i < this.data.Length; i++)
            {
                if ((this.data[i] & array.data[i]) != 0)
                    return false;
            }
            return true;
        }
        /// <summary> Returns whether all bits set in the specified array are set in the this array as well. </summary>
        public bool Contains(BitArray array)
        {
            Contract.Requires(array != null);
            Contract.Requires(this.Length == array.Length);

            for (int i = 0; i < this.data.Length; i++)
            {
                if ((this.data[i] & array.data[i]) != array.data[i])
                    return false;
            }
            return true;
        }
        /// <summary> Returns whether all bits set in the specified bit sequence are set in the this array as well. </summary>
        public bool Contains(IReadOnlyList<bool> bitSequence)
        {
            Contract.Requires(bitSequence != null);
            Contract.Requires(bitSequence.Count == checked((int)this.Length));

            for (int i = 0; i < checked((int)this.Length); i++)
            {
                if (bitSequence[i] && !this[i])
                    return false;
            }
            return true;
        }
        /// <summary> Gets whether no bit is set. Returns true when this array has length 0. </summary>
        public bool IsEmpty()
        {
            return this.data.All(internalElement => internalElement == 0);
        }
        /// <summary> Gets whether all bits are set. Returns true when this array has length 0. </summary>
        public bool IsFull()
        {
            for (int i = 0; i < this.data.Length - 1; i++)
            {
                if (this.data[i] != ulong.MaxValue)
                    return false;
            }
            if (data.Length != 0)
            {
                int bitCountThatHasToBeSetOnLastInternalElement = (int)(this.Length % bitCountPerInternalElement);
                ulong fullBitPatternOfLastIntervalElement = bitCountThatHasToBeSetOnLastInternalElement == bitCountPerInternalElement
                                                                    ? ulong.MaxValue
                                                                    : (1UL << bitCountThatHasToBeSetOnLastInternalElement) - 1;//would correctly overflow if bitCountThatHasToBeSetOnLastInternalElement == bitCountPerInternalElement

                ulong lastInternalElement = data.Last();
                if (lastInternalElement != fullBitPatternOfLastIntervalElement)
                    return false;
            }


            return true;
        }

        /// <summary> Performs the OR operation on this array with the specified array. </summary>
        [DebuggerHidden]
        public void Or(BitArray array)
        {
            Contract.Requires(array != null);
            Contract.Requires(array.Length == this.Length);

            Contract.Assert(array.data.Length == this.data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] |= array.data[i];
            }
        }
        /// <summary> Performs the OR operation on this array with the specified array. </summary>
        [DebuggerHidden]
        public void Or(ImmutableBitArray array)
        {
            Contract.Requires(!ReferenceEquals(array, null));
            Contract.Requires(array.Length == this.Length);

            Or(array.data);
        }
        /// <summary> Performs the OR operation on this array with the specified bit sequence. </summary>
        [DebuggerHidden]
        public void Or(IReadOnlyList<bool> bitSequence)
        {
            Contract.Requires(bitSequence != null);
            Contract.Requires(bitSequence.Count == checked((int)this.Length));

            for (int i = 0; i < bitSequence.Count; i++)
            {
                this[i] |= bitSequence[i];
            }
        }
        /// <summary> Performs the AND operation on this array with the specified array. </summary>
        public void And(BitArray array)
        {
            Contract.Requires(array != null);
            Contract.Requires(array.Length == this.Length);

            Contract.Assert(array.data.Length == this.data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] &= array.data[i];
            }
        }
        /// <summary> Performs the AND operation on this array with the specified array. </summary>
        public void And(ImmutableBitArray array)
        {
            Contract.Requires(!ReferenceEquals(array, null));
            Contract.Requires(array.Length == this.Length);

            And(array.data);
        }
        /// <summary> Performs the AND operation on this array with the specified bit sequence. </summary>
        public void And(IReadOnlyList<bool> bitSequence)
        {
            Contract.Requires(bitSequence != null);
            Contract.Requires(bitSequence.Count == checked((int)this.Length));

            for (int i = 0; i < bitSequence.Count; i++)
            {
                this[i] &= bitSequence[i];
            }
        }
        /// <summary> Gets a clone of the current bit array and sets the bit at the specified index to the specified value. </summary>
        /// <param name="index"> The index of the bit to set in the new array. </param>
        /// <param name="value"> The value of the bit to set in the new array. </param>
        public BitArray With(int index, bool value = true)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            return this.With((ulong)index, value);
        }
        /// <summary> Gets a clone of the current bit array and sets the bit at the specified index to the specified value. </summary>
        /// <param name="index"> The index of the bit to set in the new array. </param>
        /// <param name="value"> The value of the bit to set in the new array. </param>
        public BitArray With(ulong index, bool value = true)
        {
            Contract.Requires(0 <= index);
            Contract.Requires(index < this.Length);

            var result = this.Clone();
            result[index] = value;

            Contract.Ensures(result.Length == this.Length);
            return result;
        }

        /// <summary> Returns a new bit array containing all bits of the specified bit arrays. </summary>
        public static BitArray operator |(BitArray a, BitArray b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(a.Length == b.Length);

            var result = a.Clone();
            result.Or(b);
            return result;
        }
        /// <summary> Returns a new bit array containing the overlapping bits of the specified bit arrays. </summary>
        public static BitArray operator &(BitArray a, BitArray b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(a.Length == b.Length);

            var result = a.Clone();
            result.And(b);
            return result;
        }
        /// <summary> Returns a new bit array containing the bits of the first without the bits of the second: so this is equivalent to a &amp;(~b) </summary>
        public static BitArray operator -(BitArray a, BitArray b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(a.Length == b.Length);

            var result = new BitArray(a.Length);
            for (int i = 0; i < result.data.Length; i++)
            {
                result.data[i] = a.data[i] & ~b.data[i];
            }
            return result;
        }

        #region Supported IList<bool> Members

        int IReadOnlyCollection<bool>.Count
        {
            get
            {
                Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
                return (int)Length;
            }
        }
        int ICollection<bool>.Count
        {
            get
            {
                return ((IReadOnlyCollection<bool>)this).Count;
            }
        }
        [DebuggerHidden]
        public void CopyTo(bool[] array, int arrayIndex)
        {
            Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
            for (int i = 0; i < (int)this.Length; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }
        [DebuggerHidden]
        public void CopyTo(ulong[] array, int arrayIndex)
        {
            this.data.CopyTo(array, arrayIndex);
        }
        [DebuggerHidden]
        public void CopyTo(Span<byte> array, int arrayIndex)
        {
            const int bytesPerULong = 8;
            const int bitsPerULong = 64;
            const int bitsPerByte = 8;

            if (this.Length == 0)
                return;
            ulong neededNumberOfBytes = this.Length == 0 ? 0 : ((this.Length - 1) / bitsPerByte);
            if (array.Length < checked((int)((ulong)arrayIndex + neededNumberOfBytes)))
                throw new IndexOutOfRangeException("Specified array too small");

            int i;
            for (i = 0; i < checked((int)(this.Length / bitsPerULong)); i++)
            {
                for (int j = 0; j < bytesPerULong; j++)
                {
                    array[arrayIndex + i * bytesPerULong + j] = (byte)(data[i] >> (j * bitsPerByte));
                }
            }
            for (int j = 0; j < checked((int)((this.Length % bitsPerULong) / bytesPerULong)); j++)
            {
                array[arrayIndex + i * bytesPerULong + j] = (byte)(data[i] >> (j * bitsPerByte));
            }
            // << shifts to higher order bits
            // mask the last byte that may contain unzeroed data:
            int extraBits = (int)(this.Length % bitsPerByte);
            if (extraBits != 0)
            {
                byte mask = (byte)-(byte.MaxValue >> extraBits);
                array[^1] &= mask;
            }
        }
        [DebuggerHidden]
        public void CopyTo(BitArray array, ulong sourceStartBitIndex, ulong length, ulong destStartBitIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            Contract.Assert<NotImplementedException>(sourceStartBitIndex + length <= int.MaxValue);
            if (sourceStartBitIndex + length > (ulong)this.Length) throw new ArgumentOutOfRangeException("sourceStartBitIndex + length");
            Contract.Assert<NotImplementedException>(destStartBitIndex + length <= int.MaxValue);
            if (destStartBitIndex + length > (ulong)array.Length) throw new ArgumentOutOfRangeException("destStartBitIndex + length");

            CopyTo(array.data, sourceStartBitIndex, length, destStartBitIndex);
        }
        [DebuggerHidden]
        internal void CopyTo(ulong[] array, ulong sourceStartBitIndex, ulong length, ulong destStartBitIndex)
        {
            BitTwiddling.CopyBitsTo(this.data, array, sourceStartBitIndex, destStartBitIndex, length: length);
        }
        [DebuggerHidden]
        public IEnumerator<bool> GetEnumerator()
        {
            return Enumerable.Range(0, checked((int)this.Length)).Select(i => this[i]).GetEnumerator();
        }
        [DebuggerHidden]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        [DebuggerHidden]
        public bool IsReadOnly
        {
            get { return false; }
        }
        [DebuggerHidden]
        public long IndexOf(ulong item, int? itemLength = null, ulong startBitIndex = 0)
        {
            return BitTwiddling.IndexOfBits(this.data, item, itemLength, startBitIndex, this.Length);
        }
        /// <summary>
        /// Returns the last of the matches consecutive with the first match after or at <paramref name="startBitIndex"/>.
        /// </summary>
        public long IndexOfLastConsecutive(ulong item, int? itemLength = null, ulong startBitIndex = 0)
        {
            return BitTwiddling.IndexOfBits(this.data, new[] { item }, itemLength, startBitIndex, this.Length, returnLastConsecutive: true).BitIndex;
        }
        public bool IsAt(ulong index, ulong item, int? itemLength = null)
        {
            ulong length = (ulong)(itemLength ?? 64);
            var tempWrapper = new BitArray(new ulong[] { item }, length);
            return this.BitSequenceEqual(tempWrapper, index, length);
        }
        public bool BitSequenceEqual(BitArray other)
        {
            return BitSequenceEqual(other, 0, this.Length);
        }
        public bool BitSequenceEqual(BitArray other, ulong sourceStartBitIndex, ulong sourceBitLength)
        {
            var sourceSegment = new BitArrayReadOnlySegment(this, sourceStartBitIndex, sourceBitLength);
            return sourceSegment.Equals(other);
        }
        public bool BitSequenceEqual(BitArrayReadOnlySegment other)
        {
            return BitSequenceEqual(other, 0, this.Length);
        }
        public bool BitSequenceEqual(BitArrayReadOnlySegment other, ulong sourceStartBitIndex, ulong sourceBitLength)
        {
            return this.data.BitSequenceEqual(other.data.data, sourceStartBitIndex, other.start, other.Length, sourceBitLength, other.data.Length);
        }
        public (long BitIndex, int ItemIndex) IndexOfAny(IReadOnlyList<ulong> items, int? itemLength = null, ulong startIndex = 0)
        {
            return BitTwiddling.IndexOfBits(this.data, items, itemLength, startIndex, (ulong)this.Length);
        }
        public void Insert(ulong index, bool value)
        {
            this.data = BitTwiddling.InsertBits(this.data, new[] { index }, new[] { value }, (ulong)this.Length);
            this.Length++;
        }
        /// <summary>
        /// Inserts bits into the specified bit source.
        /// </summary>
        /// <param name="source"> The bits to insert in (is left unmodified). </param>
        /// <param name="sortedBitIndices"> The indices of the bits to insert. Must be non-decreasing. </param>
        /// <param name="values"> The bits to insert. </param>
        /// <param name="sourceLengthInBits">The length of the source. Defaults to <code>source * 64.</code></param>
        /// <returns>a new array with the bits inserted. </returns>
        public void InsertRange(ulong[] sortedIndices, bool[] values)
        {
            // there's still PERF to be gained by not creating a new array if it would fit, by implementing `ref this.data`
            this.data = BitTwiddling.InsertBits(this.data, sortedIndices, values, (ulong)this.Length);
            this.Length += (ulong)sortedIndices.Length;
        }
        public void RemoveAt(ulong index)
        {
            this.RemoveAt(new[] { index });
        }
        public void RemoveAt(params ulong[] indices)
        {
            Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
            if (indices.Length == 0)
                return;
            if (indices.Length > (int)this.Length)
                throw new ArgumentOutOfRangeException(nameof(indices), "More indices specified than bits");
            // this is a very inefficient implementation
            // use constructor to convert to ulong[]
            int[] indicesInts = indices.Map(u => checked((int)u));
            var bitArray = new BitArray(this.AsEnumerable().ExceptAt(indicesInts), this.Length - (ulong)indices.Length);
            bitArray.CopyTo(this.data, 0); // overwrite current; will always fit
            this.Length -= (ulong)indices.Length;
        }
        #endregion

        #region Not supported IList<bool> Members

        void ICollection<bool>.Add(bool item)
        {
            throw new NotSupportedException();
        }
        void ICollection<bool>.Clear()
        {
            throw new NotSupportedException();
        }
        bool ICollection<bool>.Contains(bool item)
        {
            throw new NotImplementedException();
        }
        int IList<bool>.IndexOf(bool item)
        {
            throw new NotSupportedException();
        }
        bool ICollection<bool>.Remove(bool item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Equality Members

        public static bool operator ==(BitArray? a, BitArray? b)
        {
            if (ReferenceEquals(a, null))
            {
                return ReferenceEquals(b, null);
            }
            return a.Equals(b);
        }
        public static bool operator !=(BitArray? a, BitArray? b)
        {
            return !(a == b);
        }
        public override bool Equals(object? obj)
        {
            return Equals(obj as BitArray);
        }
        public bool Equals(BitArray? other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(other, this))
                return true;

            if (this.Length != other.Length)
                return false;

            int fullUlongDataLength = checked((int)(this.Length / 64));

            if (!this.data.Take(fullUlongDataLength).SequenceEqual(other.data.Take(fullUlongDataLength)))
                return false;

            ulong getLast(ulong[] data)
            {
                return data[fullUlongDataLength].Mask(0, (int)(this.Length % 64));
            }

            if ((this.Length % 64) != 0)
            {
                var thisLast = getLast(this.data);
                var otherLast = getLast(other.data);

                return thisLast == otherLast;
            }
            return true;

        }

        public bool Equals(BitArrayReadOnlySegment segment)
        {
            return segment.Equals(this);
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        #endregion

        public BitReader ToBitReader()
        {
            return new BitReader(this);
        }

        void IList<bool>.Insert(int index, bool item)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Insert((ulong)index, item);
        }

        void IList<bool>.RemoveAt(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            RemoveAt((ulong)index);
        }
    }

    public static class BitArrayExtensions
    {
        /// <summary> Gets whether none of the specified bit arrays have any bit overlap. The empty sequence is considered disjoint. </summary>
        public static bool AreDisjoint(this IEnumerable<BitArray> arrays)
        {
            Contract.Requires(arrays != null);

            bool first = true;
            BitArray? accumulation = null;
            foreach (var array in arrays)
            {
                if (first)
                {
                    accumulation = array.Clone();
                    first = false;
                }
                else
                {
                    if (!accumulation!.IsDisjointFrom(array))
                    {
                        return false;
                    }

                    accumulation |= array;
                }
            }
            return true;
        }
        /// <summary> Gets whether none of the specified bit arrays have any bit overlap. The empty sequence is considered disjoint. </summary>
        public static bool AreDisjoint(this IEnumerable<ImmutableBitArray> arrays)
        {
            Contract.Requires(arrays != null);
            Contract.LazilyRequires(ref arrays, NotNull);

            return arrays.Select(array => array.data).AreDisjoint();
        }
        /// <summary> Performs the OR operation on all specified arrays. </summary>
        /// <param name="arrays"> The bits to OR together. Must be commensurate. </param>
        public static BitArray Or(this IEnumerable<BitArray> arrays)
        {
            Contract.Requires(arrays != null);
            EnsureSingleEnumerationDEBUG(ref arrays);
            Contract.RequiresForAll(arrays, NotNull);
            Contract.Requires(arrays.Select(array => array.Length).AreEqual());
            Contract.Requires(arrays.Any());

            BitArray? result = null;
            foreach (BitArray array in arrays)
            {
                if (result == null)
                    result = array.Clone();
                else
                    result.Or(array);
            }

            return result!;
        }
        /// <summary> Performs the OR operation on all specified bit sequences. </summary>
        /// <param name="bitSequences"> The bit sequences to OR together. Must be commensurate. </param>
        public static BitArray Or(this IEnumerable<IReadOnlyList<bool>> bitSequences)
        {
            Contract.Requires(bitSequences != null);
            EnsureSingleEnumerationDEBUG(ref bitSequences);
            Contract.RequiresForAll(bitSequences, NotNull);
            Contract.Requires(bitSequences.Select(array => array.Count).AreEqual());

            BitArray? result = null;
            foreach (IReadOnlyList<bool> array in bitSequences)
            {
                if (result == null)
                    result = new BitArray(array);
                else
                    result.Or(array);
            }

            return result!;
        }

        /// <summary> Returns <see param="bitCount"/> ones followed by zeroes (least significant to most). </summary>
        internal static ulong LowerBitsMask(int bitCount)
        {
            return ulong.MaxValue >> (64 - bitCount);
        }
        /// <summary> Returns zeroes ending on <see param="bitCount"/> ones (least significant to most). </summary>
        internal static ulong UpperBitsMask(int bitCount)
        {
            return ~UpperBitsUnmask(bitCount);
        }
        /// <summary> Returns <see param="bitCount"/> zeroes followed by ones (least significant to most). </summary>
        internal static ulong LowerBitsUnmask(int bitCount)
        {
            return ~LowerBitsMask(bitCount);
        }
        /// <summary> Returns ones ending on <see param="bitCount"/> zeroes (least significant to most). </summary>
        internal static ulong UpperBitsUnmask(int bitCount)
        {
            return ulong.MaxValue << bitCount;
        }

        /// <summary>
        /// Sets the specified data at the specified index.
        /// </summary>
        public static void Set(this BitArray dest, ulong data, int dataLength, ulong insertionIndex)
        {
            // easy (but imperformant) way of setting ulong into bitarray:
            var wrappedData = new BitArray(new ulong[] { data }, length: dataLength);
            wrappedData.CopyTo(dest, 0, (ulong)dataLength, insertionIndex);
        }
        public static BitArray Insert(this BitArray array, ulong data, int dataLength, ulong insertionIndex)
        {
            if (array == null!) throw new ArgumentNullException(nameof(array));
            if (dataLength < 0 || dataLength > 64) throw new ArgumentOutOfRangeException(nameof(dataLength));
            if (insertionIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(insertionIndex));

            ulong destLength = (ulong)BitArray.ComputeInternalStructureSize(array.Length + (ulong)dataLength);
            var dest = new ulong[checked((int)destLength)];
            array.CopyTo(dest, 0UL, insertionIndex, 0);
            array.CopyTo(dest, insertionIndex, array.Length - insertionIndex, (ulong)dataLength);

            var result = new BitArray(dest, length: array.Length + (ulong)dataLength);

            result.Set(data, dataLength, insertionIndex);
            return result;
        }
        public static BitArray Prepend(this BitArray array, ulong data, int dataLength)
        {
            return array.Insert(data, dataLength, 0UL);
        }
        public static BitArray Append(this BitArray array, ulong data, int dataLength)
        {
            return array.Insert(data, dataLength, array.Length);
        }
        /// <summary>
        /// A perf optimization to prepending and appending.
        /// </summary>
        public static BitArray Wrap(this BitArray array, ulong prependData, int prependDataLength, ulong appendData, int appendDataLength)
        {
            // TODO: perf
            return array.Prepend(prependData, prependDataLength)
                        .Append(appendData, appendDataLength);
        }
        public static BitArray ToBitArray(this bool[] bits)
        {
            return new BitArray(bits);
        }
        public static BitArray ToBitArray(this IEnumerable<bool> bits)
        {
            return new BitArray(bits);
        }
    }
}
