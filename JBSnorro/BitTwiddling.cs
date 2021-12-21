using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitArray = JBSnorro.Collections.BitArray;

namespace JBSnorro
{
	/// <summary> Contains functionality around bittwiddling. </summary>
	public static class BitTwiddling
	{
		/// <summary> Gets the bit in the specified byte at the specified index. </summary>
		/// <param name="flags"> The byte representing individual bits. </param>
		/// <param name="index"> The index of the bit to get. Index 0 is least significant, 7 is most significant significant. </param>
		public static bool HasBit(this byte flags, int index)
		{
			Contract.Requires(0 <= index && index < 8);

			return (flags & (1 << index)) != 0;
		}
		/// <summary> Gets the bit in the specified byte at the specified index. </summary>
		/// <param name="flags"> The ushort representing individual bits. </param>
		/// <param name="index"> The index of the bit to get. Index 0 is least significant, 15MO is most significant significant. </param>
		public static bool HasBit(this ushort flags, int index)
		{
			Contract.Requires(0 <= index && index < 16);

			return (flags & (1 << index)) != 0;
		}
		/// <summary> Gets the bit in the specified byte at the specified index. </summary>
		/// <param name="flags"> The uint representing individual bits. </param>
		/// <param name="index"> The index of the bit to get. Index 0 is least significant, 31 is most significant significant. </param>
		public static bool HasBit(this uint flags, int index)
		{
			Contract.Requires(0 <= index && index < 32);

			return (flags & (1 << index)) != 0;
		}
		/// <summary> Gets the bitwise reversed integer of the specified integer. </summary>
		public static int ReverseBitwise(this int i)
		{
			return (int)ReverseBitwise((uint)i);
		}
		/// <summary> Gets the bitwise reversed unsigned integer of the specified unsigned integer. </summary>
		public static uint ReverseBitwise(this uint i)
		{
			uint result = 0;
			int bi = 31;//bit index to set
			while (i != 0)
			{
				result |= (i % 2) >> bi;
				bi--;
				i >>= 1;
			}
			return result;
		}

		public static int RoundDownToNearestMultipleOf(this int i, int multiple)
		{
			Contract.Requires(i >= 0);
			Contract.Requires(multiple >= 0);

			int remainder = i % multiple;

			if (remainder == 0)
				return i;

			return i - remainder;
		}
		public static int RoundUpToNearestMultipleOf(this int i, int multiple)
		{
			Contract.Requires(i >= 0);
			Contract.Requires(multiple >= 0);

			int remainder = i % multiple;

			if (remainder == 0)
				return i;

			return i - remainder + multiple;//may overflow
											//i - remainder is obviously a multiple
											//hence that + multiple is as well
		}

		public static BitArray ToBitArray(this uint flags, int capacity = 32)
		{
			Contract.Requires(0 <= capacity);
			Contract.Requires(capacity <= 32);

			var result = new BitArray(capacity);
			for (int i = 0; i < capacity; i++)
			{
				result[i] = HasBit(flags, i);
			}
			return result;
		}
		public static ImmutableBitArray ToImmutableBitArray(this uint flags, int capacity = 32)
		{
			return new ImmutableBitArray(flags.ToBitArray(capacity));
		}
	}
}
