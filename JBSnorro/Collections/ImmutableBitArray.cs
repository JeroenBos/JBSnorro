#nullable enable
using JBSnorro;
using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using System.Diagnostics;
using static JBSnorro.Global;

namespace JBSnorro.Collections
{
    /// <summary> Represents an immutable array of booleans, stored as bits. </summary>
    public class ImmutableBitArray : System.Collections.Generic.IReadOnlyList<bool>
	{
		/// <summary> Creates an immutable bit array that is the product of applying OR on all specified arrays. </summary>
		public static ImmutableBitArray Or(IEnumerable<BitArray> arrays)
		{
			Contract.Requires(arrays != null);

			return new ImmutableBitArray(arrays.Or(), DontClone);
		}
		/// <summary> Creates an immutable bit array that is the product of applying OR on all specified arrays. </summary>
		public static ImmutableBitArray Or(IEnumerable<ImmutableBitArray> arrays)
		{
			Contract.Requires(arrays != null);
			Contract.LazilyAssertMinimumCount(ref arrays, 1);

			return new ImmutableBitArray(arrays.Select(iba => iba.data).Or(), DontClone);
		}
		/// <summary> Gets an empty immutable bit array. </summary>
		public static ImmutableBitArray Empty { get; } = new ImmutableBitArray(0, false);

		/// <summary> Gets the internal data structure of the immutable bit array. Should only be accessed from the ImmutableBitArray and BitArrayExtensions. And should NEVER be modified. </summary>
		internal readonly BitArray data;

		/// <summary> Creates a new immutable bit array containing the specified number of bits set to the specified value. </summary>
		public ImmutableBitArray(int length, bool value)
		{
			data = new BitArray(length, value);
		}
		/// <summary> Creates a new immutable bit array containing the specified bits. </summary>
		public ImmutableBitArray(IEnumerable<bool> bits)
		{
			data = new BitArray(bits);
		}
		/// <summary> Creates a new immutable bit array containing the specified bits. </summary>
		public ImmutableBitArray(IList<bool> bits)
		{
			data = new BitArray(bits);
		}
		/// <summary> Creates a new immutable bit array containing the specified bits. </summary>
		public ImmutableBitArray(IReadOnlyList<bool> bits)
		{
			data = new BitArray(bits);
		}
		/// <summary> Creates a new immutable bit array containing the specified bits. </summary>
		public ImmutableBitArray(BitArray bits)
		{
			data = bits.Clone();
		}
		/// <summary> Creates a new immutable bit array containing true bits at the specified indices. </summary>
		public ImmutableBitArray(int length, params int[] indicesOfTrueBits)
		{
			Contract.Requires(indicesOfTrueBits != null);
			Contract.Requires(0 <= length);
			Contract.RequiresForAll(indicesOfTrueBits, i => 0 <= i && i < length);

			data = new BitArray(indicesOfTrueBits, length);
		}
		/// <summary> Creates a new immutable bit array from OR-ing the specified bit arrays together. </summary>
		public ImmutableBitArray(IEnumerable<ImmutableBitArray> bitArrays)
		{
			Contract.Requires(bitArrays != null);
			EnsureSingleEnumerationDEBUG(ref bitArrays);
			Contract.Requires(bitArrays.Count() > 0, "The specified sequence was empty");//so that the length of this new instance couldn't be obtained
			Contract.RequiresForAll(bitArrays, NotNull);
			Contract.Requires(bitArrays.Select(array => array.Length).AreEqual(), "The specified sequences aren't commensurate");

			this.data = default!; // removes warning for this ctor

			bool first = true;
			foreach (var array in bitArrays)
			{
				if (first)
				{
					this.data = array.data.Clone();
					first = false;
				}
				else
				{
					this.data.Or(array);
				}
			}
		}
		/// <summary> Creates a new immutable bit array from OR-ing the specified bit collections together. </summary>
		public ImmutableBitArray(IEnumerable<IReadOnlyList<bool>> bitsToOR)
		{
			EnsureSingleEnumerationDEBUG(ref bitsToOR);
			Contract.Requires(bitsToOR != null);
			Contract.Requires(bitsToOR.Count() > 0, "The specified sequence was empty");//so that the length of this new instance couldn't be obtained
			Contract.RequiresForAll(bitsToOR, NotNull);
			Contract.Requires(bitsToOR.Select(array => array.Count).AreEqual(), "The specified sequences aren't commensurate");

			this.data = new BitArray(bitsToOR);
		}

		/// <summary> A value that can be specified to the constructor below to indicate that the specified bit array need not be cloned to guarantee immutability. </summary>
		private static readonly object DontClone = new object();
		private ImmutableBitArray(BitArray bits, object dontClone)
		{
			Contract.Requires(bits != null);
			Contract.Requires(ReferenceEquals(dontClone, DontClone));

			this.data = bits;
		}

		/// <summary> Gets a mutable clone of this bit array. </summary>
		public BitArray ToMutable()
		{
			return data.Clone();
		}
		/// <summary> Gets whether no bit is set. Returns true when this array has length 0. </summary>
		public bool IsEmpty()
		{
			return this.data.IsEmpty();
		}
		/// <summary> Gets whether all bits are set. Returns true when this array has length 0. </summary>
		public bool IsFull()
		{
			return this.data.IsFull();
		}
		public bool IsDisjointFrom(BitArray array)
		{
			Contract.Requires(!ReferenceEquals(array, null));
			Contract.Requires(array.Length == this.Length);

			return this.data.IsDisjointFrom(array);
		}
		public bool IsDisjointFrom(ImmutableBitArray array)
		{
			Contract.Requires(!ReferenceEquals(array, null));
			Contract.Requires(array.Length == this.Length);

			return this.data.IsDisjointFrom(array.data);
		}
		public bool IsDisjointFrom(IReadOnlyList<bool> array)
		{
			Contract.Requires(array != null);
			Contract.Requires(array.Count == checked((int)this.Length));

			if (array is ImmutableBitArray iba)
			{
				return this.IsDisjointFrom(iba);
			}
			else if (array is BitArray ba)
			{
				return this.IsDisjointFrom(ba);
			}
			else
			{
				return this.Zip(array, (thisBit, otherBit) => thisBit ^ otherBit)
						   .All();
			}
		}
		/// <summary> Gets whether all bits set in the specified array are set in this instance as well. </summary>
		/// <param name="array"> The bits to check. Must be commensurate to this array. </param>
		public bool Contains(ImmutableBitArray array)
		{
			return this.data.Contains(array.data);
		}

		/// <summary> Creates a new immutable bit array, copied from this one, with one bit set differently. </summary>
		/// <param name="index"> The index of the bit where the new array will differ from this one. </param>
		/// <param name="value"> The value of the bit in the new array. </param>
		public ImmutableBitArray With(int index, bool value = true)
		{
			return new ImmutableBitArray(data.With(index, value), DontClone);
		}

		#region IReadOnlyList<bool> Members

		/// <summary> Gets the bit at the specified index. </summary>
		public bool this[int index]
		{
			[DebuggerHidden]
			get { return data[index]; }
		}
		/// <summary> Gets the number of bits in this collection. </summary>
		public ulong Length
		{
			get { return data.Length; }
		}
		int IReadOnlyCollection<bool>.Count
		{
			get
			{
				Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
				return (int)this.Length;
			}
		}
		public IEnumerator<bool> GetEnumerator()
		{
			return data.GetEnumerator();
		}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Operators

		public static ImmutableBitArray operator |(ImmutableBitArray a, ImmutableBitArray b)
		{
			Contract.Requires(!ReferenceEquals(a, null));
			Contract.Requires(!ReferenceEquals(b, null));
			Contract.Requires(a.Length == b.Length);

			return new ImmutableBitArray(a.data | b.data, DontClone);
		}
		public static ImmutableBitArray operator &(ImmutableBitArray a, ImmutableBitArray b)
		{
			Contract.Requires(!ReferenceEquals(a, null));
			Contract.Requires(!ReferenceEquals(b, null));
			Contract.Requires(a.Length == b.Length);

			return new ImmutableBitArray(a.data & b.data, DontClone);
		}
		/// <summary> Returns the first bit array without the bits set in the second: so this is equivalent to a &amp;(~b) </summary>
		public static ImmutableBitArray operator -(ImmutableBitArray a, ImmutableBitArray b)
		{
			Contract.Requires(!ReferenceEquals(a, null));
			Contract.Requires(!ReferenceEquals(b, null));
			Contract.Requires(a.Length == b.Length);

			return new ImmutableBitArray(a.data - b.data, DontClone);
		}
		public static bool operator ==(ImmutableBitArray a, BitArray b)
		{
			return a?.data == b;
		}
		public static bool operator ==(BitArray b, ImmutableBitArray a)
		{
			return a?.data == b;
		}
		public static bool operator ==(ImmutableBitArray a, ImmutableBitArray b)
		{
			if (ReferenceEquals(a, null))
				return ReferenceEquals(b, null);
			if (ReferenceEquals(b, null))
				return false;
			return a.data == b.data;
		}
		public static bool operator !=(ImmutableBitArray a, BitArray b)
		{
			return !(a == b);
		}
		public static bool operator !=(BitArray b, ImmutableBitArray a)
		{
			return !(a == b);
		}

		public static bool operator !=(ImmutableBitArray a, ImmutableBitArray b)
		{
			return !(a == b);
		}

		#endregion

		#region Equality Members

		public override bool Equals(object? obj)
		{
			throw new NotImplementedException();//Depends on whether you want equality to bit array as well. I.e. is immutability equatable?
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();//to prevent C# warning
		}

		#endregion


		public override string ToString()
		{
			if (ReferenceEquals(this, Empty))
				return "Empty ImmutableBitArray";
			return $"length : {Length}";
		}
	}
}
