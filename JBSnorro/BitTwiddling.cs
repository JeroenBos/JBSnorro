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
			const int N = 32;
			uint result = 0;
			for (int bi = N - 1; i != 0; bi--)
			{
				result |= (i & 1) >> bi;
				i >>= 1;
			}
			return result;
		}
		public static long ReverseBitwise(this long i)
		{
			return (long)ReverseBitwise((ulong)i);
		}
		/// <summary> Gets the bitwise reversed unsigned long of the specified unsigned long. </summary>
		public static ulong ReverseBitwise(this ulong i)
		{
			const int N = 64;
			ulong result = 0;
			for (int bi = N - 1; i != 0; bi--)
			{
				result |= (i & 1) >> bi;
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

			return i - remainder + multiple;// may overflow
											// i - remainder is obviously a multiple
											// hence that + multiple is as well
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

		/// <summary>
		/// Inserts bits into the specified bit source.
		/// </summary>
		/// <param name="source"> The bits to insert in (is left unmodified). </param>
		/// <param name="sortedBitIndices"> The indices of the bits to insert. Must be non-decreasing. </param>
		/// <param name="values"> The bits to insert. </param>
		/// <param name="sourceLengthInBits">The length of the source. Defaults to <code>source * 64.</code></param>
		/// <returns>a new array with the bits inserted. </returns>
		public static ulong[] InsertBits(this ulong[] source, int[] sortedBitIndices, bool[] values, ulong? sourceLengthInBits = null)
		{
			const int nLength = 64;

			if (sortedBitIndices.Length == 0)
				return source;
			if (!sortedBitIndices.AreIncreasing())
				throw new ArgumentException($"{nameof(sortedBitIndices)} must be monotonically increasing", nameof(sortedBitIndices));
			if (sortedBitIndices[0] < 0)
				throw new IndexOutOfRangeException($"{nameof(sortedBitIndices)} indices must not be negative");
			if (source is ICollection<ulong> collection)
			{
				if (sortedBitIndices[^1] > collection.Count * nLength)
					throw new IndexOutOfRangeException($"{nameof(sortedBitIndices)} indices must not be after the source length (the very end is allowed)");
			}
			else
				throw new NotImplementedException("Expected ICollection<ulong>");

			if (values.Length != sortedBitIndices.Length)
				throw new ArgumentException($"{nameof(values)} must contain the same number of elements as {nameof(sortedBitIndices)}");

			ulong bitCount = sourceLengthInBits ?? ((ulong)collection.Count * nLength);
			if (bitCount > (ulong)collection.Count * nLength)
				throw new IndexOutOfRangeException(nameof(sourceLengthInBits));



			ulong[] dest = CreateDest(bitCount, source.Length, values.Length);

			foreach (var (startBitIndex, destBitIndex, length, bit) in GetRanges(sortedBitIndices, values, bitCount))
			{
				CopyBits(source, dest, startBitIndex, destBitIndex, length);
				if (bit != null)
				{
					SetBit(dest, destBitIndex + length, bit.Value);
				}
			}
			return dest;


			static ulong[] CreateDest(ulong bitCount, int sourceCount, int valuesLength)
			{
				ulong uselessBitCount = (ulong)sourceCount * nLength - bitCount;
				Contract.Assert(uselessBitCount >= 0);
				ulong requiredBitCount = (ulong)sourceCount * nLength + (ulong)valuesLength;
				ulong requiredCount = ((requiredBitCount - uselessBitCount) + (nLength - 1)) / nLength;
				var newLength = requiredCount;
				return new ulong[newLength];
			}
			static IEnumerable<(ulong SourceStartBitIndex, ulong DestBitIndex, ulong Length, bool? Value)> GetRanges(IEnumerable<int> sortedBitIndices, bool[] values, ulong bitCount)
			{
				// var sortedSourceBitIndices = sortedBitIndices.Select((val, index) => (ulong)(val - index));
				var sortedSourceBitIndices = sortedBitIndices.Select(i => (ulong)i);

				// destBitIndex is the index at which the bit is to be set start pasting. The bit is to be set at destBitIndex + length
				ulong previousSourceBitIndex = 0;
				uint i = 0;
				ulong dest = 0;
				foreach (var sourceBitIndex in sortedSourceBitIndices)
				{
					yield return (previousSourceBitIndex, dest, sourceBitIndex - previousSourceBitIndex, values[i]);
					dest += (sourceBitIndex - previousSourceBitIndex);
					previousSourceBitIndex = sourceBitIndex;
					i++;
					dest += 1;
				}
				yield return (previousSourceBitIndex, dest, bitCount - previousSourceBitIndex, null);
			}
			static void CopyBits(ulong[] source, ulong[] dest, ulong sourceStart, ulong destStart, ulong length)
			{
				if (sourceStart + length > (ulong)source.Length * nLength) throw new ArgumentOutOfRangeException(nameof(sourceStart));
				if (destStart + length > (ulong)dest.Length * nLength) throw new ArgumentOutOfRangeException(nameof(destStart));
				if (length > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(length));

				ulong currentSource = sourceStart;
				ulong currentDest = destStart;
				long remaining = (long)length;
				while (remaining > 0)
				{
					ulong nextDestUlongBoundary = currentDest + (nLength - (currentDest % nLength));
					if (nextDestUlongBoundary < currentDest) throw new Exception();
					int sourceIndex = (int)(currentSource / nLength);
					ulong source1 = source[sourceIndex];
					ulong source2 = sourceIndex + 1 == source.Length ? 0UL : source[sourceIndex + 1];
					uint diff = (uint)(nextDestUlongBoundary - currentDest); // Math.Min((ulong)remaining, nextDestUlongBoundary - currentDest);
					ref var dst = ref dest[(int)(currentDest / nLength)];
					Copy(source1, source2, ref dst, (int)(currentSource % nLength), (int)(currentDest % nLength), diff);

					remaining -= diff;
					currentDest += diff;
					currentSource += diff;
				}


				static void Copy(ulong source1, ulong source2, ref ulong dest, int index, int destIndex, uint length)
				{
					ulong orig1 = source1;
					ulong orig2 = source2;
					if (index < 0 || index >= nLength) throw new ArgumentOutOfRangeException(nameof(index));
					if (length < 0 || length > int.MaxValue || length > 2 * nLength) throw new ArgumentOutOfRangeException(nameof(length));
					if (destIndex + length > nLength) throw new ArgumentOutOfRangeException(nameof(destIndex));
					if (index + length > 2 * nLength) throw new ArgumentOutOfRangeException(nameof(index));

					if (index == destIndex)
					{
						// this is supposed to result in source2 <<= 64, but that's a no-op (which I didn't expect)
						source2 = 0;
					}
					else if (index >= destIndex)
					{
						source1 >>= (index - destIndex);
						source2 <<= 64 - (index - destIndex);
					}
					else
					{
						source1 <<= (destIndex - index);
						source2 >>= 64 - (destIndex - index);
					}
					ulong mask = CreateULongMask(destIndex, destIndex + (int)length);
					ulong newDest = (source1 | source2) & mask;

					dest &= ~mask;
					dest |= newDest;
				}

			}
			static void SetBit(ulong[] dest, ulong bitIndex, bool value)
			{
				ulong flag = 1UL << (int)(bitIndex % nLength);
				if (value)
					dest[bitIndex / nLength] |= flag;
				else
					dest[bitIndex / nLength] &= ~flag;
			}
		}
		// I want a remainder that returns the positive remainder
		static int PositiveRemainder(int dividend, int divisor, bool strictlyPositive = false)
		{
			if (divisor < 0)
				throw new NotImplementedException();
			int remainder = dividend % divisor;
			if (remainder == 0)
				return strictlyPositive ? divisor : 0;
			if (dividend > 0)
				return remainder;

			remainder += divisor;
			if (remainder == 0)
				return strictlyPositive ? divisor : 0;
			return remainder;
		}
		static int ClearLowBits(int bits, int numberOfBits, int nLength)
		{
			int mask = (byte)(byte.MaxValue << numberOfBits);
			return bits & mask;
		}
		static int ClearHighBits(int bits, int numberOfBits, int nLength)
		{
			int mask = (byte.MaxValue >> (nLength - numberOfBits));
			return bits & mask;
		}
		static ulong ClearLowBits(ulong bits, int numberOfBits)
		{
			ulong mask = (ulong.MaxValue << numberOfBits);
			return bits & mask;
		}
		static ulong ClearHighBits(ulong bits, int numberOfBitsToClear)
		{
			ulong mask = ulong.MaxValue >> numberOfBitsToClear;
			return bits & mask;
		}
		/// <param name="until">exclusive.</param>
		static ulong CreateULongMask(int from, int until)
		{
			if (from < 0 || from > 64) throw new ArgumentOutOfRangeException(nameof(from));
			if (until < from || until > 64) throw new ArgumentOutOfRangeException(nameof(until));

			return ClearHighBits(ClearLowBits(ulong.MaxValue, from), 64 - until);
		}
		/// <summary>
		/// Removes bits at specified indices.
		/// </summary>
		/// <param name="bytes">The source sequence to remove from.</param>
		/// <param name="sortedBitIndices"> The indices of the bits to remove. </param>
		/// <param name="bytesLengthInBits"> The length of the bytes sequence. Defaults to <code>8 * bytes.Count()</code>.</param>
		public static byte[] RemoveBits(this IEnumerable<byte> bytes, int[] sortedBitIndices, int? bytesLengthInBits = null)
		{
			BitArray bitArray;
			if (bytesLengthInBits == null)
				bitArray = new BitArray(bytes);
			else
				bitArray = new BitArray(bytes, bytesLengthInBits.Value);
			bitArray.RemoveAt(sortedBitIndices);
			var result = new byte[bitArray.Length / 8 + ((bitArray.Length % 8) == 0 ? 0 : 1)];
			bitArray.CopyTo(result.AsSpan(), 0);
			return result;
		}

		/// <summary> Gets the bit index in the specified data of the specified item. </summary>
		public static long IndexOfBits(IEnumerable<ulong> data, ulong item, int? itemLength = null, ulong startIndex = 0, ulong? dataLength = null)
		{
			return IndexOfBits(data, new[] { item }, itemLength, startIndex, dataLength).BitIndex;
		}
		/// <summary> Gets the first bit index in the specified data of any of the specified equilong items. </summary>
		public static (long BitIndex, int ItemIndex) IndexOfBits(IEnumerable<ulong> data, IReadOnlyList<ulong> items, int? itemLength = null, ulong startIndex = 0, ulong? dataLength = null)
		{
			const int N = 64;
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (items == null) throw new ArgumentNullException(nameof(items));
			if (items.Count == 0) throw new ArgumentException(nameof(items));
			if (itemLength != null && (itemLength < 0 || itemLength > N)) throw new ArgumentOutOfRangeException(nameof(itemLength));
			if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
			bool hasCount = data.TryGetNonEnumeratedCount(out int count);
			if (hasCount && startIndex > N * (ulong)count) throw new ArgumentOutOfRangeException(nameof(startIndex));
			itemLength ??= N;
			dataLength ??= hasCount ? (ulong)(N * count) : null;

			long bitIndex = 0;
			int elementIndex = 0;
			foreach (var ((element, nextElement), isLast) in data.Append(0UL).Windowed2().WithIsLast())
			{
				while (Fits(bitIndex, elementIndex, itemLength.Value, dataLength, isLast))
				{
					for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
					{
						if (IsMatch(bitIndex, element, nextElement, elementIndex, items[itemIndex], itemLength.Value))
							return (bitIndex, itemIndex);
					}
					bitIndex++;
				}
				elementIndex++;
			}
			return (-1, -1);


			static bool IsMatch(long bitIndex, ulong element, ulong nextElement, int elementIndex, ulong item, int itemLength)
			{
				int start = (int)(bitIndex - N * elementIndex);
				Contract.Assert(0 <= start && start < N);

				ulong aligned = TakeBits(element, nextElement, start, end: start + itemLength);
				return aligned == item;
			}
			static bool Fits(long bitIndex, int elementIndex, int itemLength, ulong? dataLength, bool isLast)
			{
				// if we should just go to the next data element even though it fits, report that it doesn't fit
				if (bitIndex >= N * (elementIndex + 1))
				{
					return false;
				}

				ulong minDataLength = dataLength ?? (N * (ulong)(elementIndex + (isLast ? 1 : 2)));
				ulong minRequiredLength = (ulong)(bitIndex + itemLength);
				return minRequiredLength <= minDataLength;
			}
		}

		public static ulong TakeBits(ulong first, ulong second, int start, int end)
		{
			const int N = 64;
			Contract.Assert(0 <= start);
			Contract.Assert(start <= end);
			Contract.Assert(end <= 2 * N);

			ulong firstPart, secondPart;
			if (end > N)
			{
				firstPart = TakeBits(first, start, N, 0);
				secondPart = TakeBits(second, 0, end - N, N - start);
			}
			else
			{
				firstPart = TakeBits(first, start, end, 0);
				secondPart = 0;
			}
			ulong result = firstPart | secondPart;
			return result;

			static ulong TakeBits(ulong bits, int start, int end, int destIndex)
			{
				Contract.Assert(0 <= start);
				Contract.Assert(start <= end);
				Contract.Assert(end <= N);
				Contract.Assert(0 <= destIndex && destIndex <= N);

				ulong shifted;
				if (start < destIndex)
				{
					shifted = bits << (destIndex - start);
				}
				else
				{
					shifted = bits >> (start - destIndex);
				}
				int length = end - start;
				int numberToBitsToKeep = destIndex + length;
				Contract.Assert(numberToBitsToKeep <= N);
				ulong result = ClearHighBits(shifted, numberOfBitsToClear: N - numberToBitsToKeep);
				return result;
			}

		}


	}

}
