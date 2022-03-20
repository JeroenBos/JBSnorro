using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JBSnorro.Global;

namespace JBSnorro.Collections
{
	/// <summary> Represents the same idea as the BCL <see cref="System.Collections.BitArray"/>, 
	/// only it derives from <see cref="System.Collections.Generic.IList{T}"/> and <see cref="System.Collections.Generic.IReadOnlyList{T}"/>. </summary>
	public sealed class BitArray : IList<bool>, IReadOnlyList<bool>
	{
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
		internal static int ComputeInternalStructureSize(int bitCount)
		{
			int remainer;
			int division = Math.DivRem(bitCount, bitCountPerInternalElement, out remainer);
			return division + (remainer == 0 ? 0 : 1);
		}

		private ulong[] data;
		/// <summary> Gets the whether the flag at the specified index in this array is set. </summary>
		public bool this[int index]
		{
			[DebuggerHidden]
			get
			{
				Contract.Requires<IndexOutOfRangeException>(0 <= index);
				Contract.Requires<IndexOutOfRangeException>(index < Length);

				int bitIndex;
#if DEBUG
				int dataIndex;
				ToInternalAndBitIndex(index, out dataIndex, out bitIndex);
#endif
				return (data[ToInternalAndBitIndex(index, out bitIndex)] & (1UL << bitIndex)) != 0;
			}
			[DebuggerHidden]
			set
			{
				Contract.Requires<IndexOutOfRangeException>(0 <= index);
				Contract.Requires<IndexOutOfRangeException>(index < Length);

				if (value)
				{
					int dataIndex, bitIndex;
					ToInternalAndBitIndex(index, out dataIndex, out bitIndex);
					data[dataIndex] |= 1UL << bitIndex;
				}
			}
		}
		/// <summary> Gets the number of bits in this bit array. </summary>
		public int Length { get; private set; }

		/// <summary> Creates a new empty bit array. </summary>
		public BitArray()
		{
			data = EmptyCollection<ulong>.Array;
		}
		/// <summary> Creates a new empty bit array containing the specified number of the specified value. </summary>
		[DebuggerHidden]
		public BitArray(int length, bool defaultValue = false)
		{
			data = new ulong[ComputeInternalStructureSize(length)];
			Length = length;
			if (defaultValue)
			{
				for (int i = 0; i < length; i++)
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
		/// <summary> Creates a new bit array from the specified bits with prespecified length. </summary>
		private BitArray(IEnumerable<bool> bits, int count)
		{
			Contract.Requires(bits != null);
			Contract.Requires(count >= 0);
			Contract.LazilyAssertCount(ref bits, count);

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
		/// <summary> Creates a new bit array from <param ref="backingData"/>, i.e. no copy is made. </summary>
		/// <param name="backingData"> The indices of bits to set to true. </param>
		/// <param name="length"> The number of bits that are considered to be set in the given data. </param>
		public BitArray(ulong[] backingData, int length)
		{
			Contract.Requires(backingData != null);
			Contract.Requires(0 <= length && length <= 64 * backingData.Length);

			this.Length = length;
			this.data = backingData;
		}

		/// <summary> Gets a clone of this bit array. </summary>
		[DebuggerHidden]
		public BitArray Clone()
		{
			return new BitArray((IReadOnlyList<bool>)this);
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
			Contract.Requires(bitSequence.Count == this.Length);

			for (int i = 0; i < this.Length; i++)
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
				int bitCountThatHasToBeSetOnLastInternalElement = this.Length % bitCountPerInternalElement;
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
			Contract.Requires(bitSequence.Count == this.Length);

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
			Contract.Requires(bitSequence.Count == this.Length);

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
			get { return Length; }
		}
		int ICollection<bool>.Count
		{
			get { return Length; }
		}
		public void CopyTo(bool[] array, int arrayIndex)
		{
			for (int i = 0; i < this.Length; i++)
			{
				array[arrayIndex + i] = this[i];
			}
		}
		public void CopyTo(ulong[] array, int arrayIndex)
		{
			this.data.CopyTo(array, arrayIndex);
		}
		public void CopyTo(Span<byte> array, int arrayIndex)
		{
			const int bytesPerULong = 8;
			const int bitsPerULong = 64;
			const int bitsPerByte = 8;

			if (this.Length == 0)
				return;
			int neededNumberOfBytes = this.Length == 0 ? 0 : ((this.Length - 1) / bitsPerByte);
			if (array.Length < arrayIndex + neededNumberOfBytes)
				throw new IndexOutOfRangeException("Specified array too small");

			int i;
			for (i = 0; i < this.Length / bitsPerULong; i++)
			{
				for (int j = 0; j < bytesPerULong; j++)
				{
					array[arrayIndex + i * bytesPerULong + j] = (byte)(data[i] >> (j * bitsPerByte));
				}
			}
			for (int j = 0; j < (this.Length % bitsPerULong) / bytesPerULong; j++)
			{
				array[arrayIndex + i * bytesPerULong + j] = (byte)(data[i] >> (j * bitsPerByte));
			}
			// << shifts to higher order bits
			// mask the last byte that may contain unzeroed data:
			int extraBits = this.Length % bitsPerByte;
			if (extraBits != 0)
			{
				byte mask = (byte)-(byte.MaxValue >> extraBits);
				array[^1] &= mask;
			}
		}
		public IEnumerator<bool> GetEnumerator()
		{
			return Enumerable.Range(0, this.Length).Select(i => this[i]).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public long IndexOf(ulong item, int? itemLength = null, ulong startIndex = 0)
		{
			return BitTwiddling.IndexOfBits(this.data, item, itemLength, startIndex, (ulong)this.Length);
		}
		public (long BitIndex, int ItemIndex) IndexOfAny(IReadOnlyList<ulong> items, int? itemLength = null, ulong startIndex = 0)
		{
			return BitTwiddling.IndexOfBits(this.data, items, itemLength, startIndex, (ulong)this.Length);
		}
		public void Insert(int index, bool value)
		{
			this.data = BitTwiddling.InsertBits(this.data, new[] { index }, new[] { value }, (ulong)this.Length);
			this.Length++;
		}
		public void InsertRange(int[] sortedIndices, bool[] values)
		{
			// there's still PERF to be gained by not creating a new array if it would fit, by implementing `ref this.data`
			this.data = BitTwiddling.InsertBits(this.data, sortedIndices, values, (ulong)this.Length);
			this.Length += sortedIndices.Length;
		}
		public void RemoveAt(int index)
		{
			this.RemoveAt(new int[] { index });
		}
		public void RemoveAt(params int[] indices)
		{
			if (indices.Length == 0)
				return;
			if (indices.Length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(indices), "More indices specified than bits");
			// this is a very inefficient implementation
			// use constructor to convert to ulong[]
			var bitArray = new BitArray(this.AsEnumerable().ExceptAt(indices), this.Length - indices.Length);
			bitArray.CopyTo(this.data, 0); // overwrite current; will always fit
			this.Length -= indices.Length;
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

		public static bool operator ==(BitArray a, BitArray b)
		{
			if (ReferenceEquals(a, null))
			{
				return ReferenceEquals(b, null);
			}
			return a.Equals(b);
		}
		public static bool operator !=(BitArray a, BitArray b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as BitArray);
		}
		public bool Equals(BitArray other)
		{
			if (ReferenceEquals(other, null))
				return false;

			return this.data.SequenceEqual(other.data);
		}
		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public static class BitArrayExtensions
	{
		/// <summary> Gets whether none of the specified bit arrays have any bit overlap. The empty sequence is considered disjoint. </summary>
		public static bool AreDisjoint(this IEnumerable<BitArray> arrays)
		{
			Contract.Requires(arrays != null);

			bool first = true;
			BitArray accumulation = null;
			foreach (var array in arrays)
			{
				if (first)
				{
					accumulation = array.Clone();
					first = false;
				}
				else
				{
					if (!accumulation.IsDisjointFrom(array))
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

			BitArray result = null;
			foreach (BitArray array in arrays)
			{
				if (result == null)
					result = array.Clone();
				else
					result.Or(array);
			}

			return result;
		}
		/// <summary> Performs the OR operation on all specified bit sequences. </summary>
		/// <param name="bitSequences"> The bit sequences to OR together. Must be commensurate. </param>
		public static BitArray Or(this IEnumerable<IReadOnlyList<bool>> bitSequences)
		{
			Contract.Requires(bitSequences != null);
			EnsureSingleEnumerationDEBUG(ref bitSequences);
			Contract.RequiresForAll(bitSequences, NotNull);
			Contract.Requires(bitSequences.Select(array => array.Count).AreEqual());

			BitArray result = null;
			foreach (IReadOnlyList<bool> array in bitSequences)
			{
				if (result == null)
					result = new BitArray(array);
				else
					result.Or(array);
			}

			return result;
		}
	}
}
