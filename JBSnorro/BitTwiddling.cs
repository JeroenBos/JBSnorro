using JBSnorro.Collections;
using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitArray = JBSnorro.Collections.Bits.BitArray;

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

        public static ulong RoundDownToNearestMultipleOf(this ulong i, ulong multiple)
        {
            ulong remainder = i % multiple;

            if (remainder == 0)
                return i;

            return i - remainder;
        }
        public static int RoundUp(this double d)
        {
            return (int)Math.Ceiling(d);
        }
        public static int RoundUp(this float f)
        {
            return (int)Math.Ceiling(f);
        }
        public static int RoundUpToNearestMultipleOf(this int i, int multiple)
        {
            Contract.Requires(i >= 0);
            Contract.Requires(multiple >= 0);

            int remainder = i % multiple;

            if (remainder == 0)
                return i;

            unchecked
            {
                return i - remainder + multiple;
            }
        }
        public static ulong RoundUpToNearestMultipleOf(this ulong i, ulong multiple)
        {
            ulong remainder = i % multiple;

            if (remainder == 0)
                return i;

            unchecked
            {
                return i - remainder + multiple;
            }
        }
        /// <summary>
        /// Returns the distance to the nearest multiple of <paramref name="multiple"/> strictly greater than <paramref name="i"/>.
        /// </summary>
        public static ulong DistanceStrictlyUpToNearestMultipleOf(this ulong i, ulong multiple)
        {
            var roundedUp = i.RoundUpToNearestMultipleOf(multiple);
            if (roundedUp == i)
            {
                return multiple;
            }
            else
            {
                return roundedUp - i;
            }
        }
        /// <summary>
        /// Returns the distance to the nearest multiple of <paramref name="multiple"/> greater than or equal to <paramref name="i"/>.
        /// </summary>
        public static ulong DistanceUpToNearestMultipleOf(this ulong i, ulong multiple)
        {
            return i.RoundUpToNearestMultipleOf(multiple) - i;
        }
        public static uint FlipBit(this uint u, int index)
        {
            uint flag = 1U << index;
            if (BitTwiddling.HasBit(u, index))
            {
                return u & ~flag;
            }
            else
            {
                return u | flag;
            }
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
        public static ulong[] InsertBits(this ulong[] source, ulong[] sortedBitIndices, bool[] values, ulong? sourceLengthInBits = null)
        {
            const int N = 64;

            if (sortedBitIndices.Length == 0)
                return source;
            if (!sortedBitIndices.AreIncreasing())
                throw new ArgumentException($"{nameof(sortedBitIndices)} must be monotonically increasing", nameof(sortedBitIndices));
            if (sortedBitIndices[0] < 0)
                throw new IndexOutOfRangeException($"{nameof(sortedBitIndices)} indices must not be negative");
            if (source is ICollection<ulong> collection)
            {
                if (sortedBitIndices[^1] > (ulong)collection.Count * N)
                    throw new IndexOutOfRangeException($"{nameof(sortedBitIndices)} indices must not be after the source length (the very end is allowed)");
            }
            else
                throw new NotImplementedException("Expected ICollection<ulong>");

            if (values.Length != sortedBitIndices.Length)
                throw new ArgumentException($"{nameof(values)} must contain the same number of elements as {nameof(sortedBitIndices)}");

            ulong sourceBitCount = sourceLengthInBits ?? ((ulong)collection.Count * N);
            if (sourceBitCount > (ulong)collection.Count * N)
                throw new IndexOutOfRangeException(nameof(sourceLengthInBits));

            ulong destBitCount = sourceBitCount + (ulong)values.Length;
            ulong[] dest = CreateDest(destBitCount, source.Length, values.Length);

            foreach (var (startBitIndex, destBitIndex, length, bit) in GetRanges(sortedBitIndices, values, sourceBitCount))
            {
                CopyBitsTo(source, dest, startBitIndex, destBitIndex, length);
                if (bit != null)
                {
                    SetBit(dest, destBitIndex + length, bit.Value);
                }
            }
            return dest;


            static ulong[] CreateDest(ulong bitCount, int sourceCount, int valuesLength)
            {
                ulong uselessBitCount = (ulong)sourceCount * N - bitCount;
                Contract.Assert(uselessBitCount >= 0);
                ulong requiredBitCount = (ulong)sourceCount * N + (ulong)valuesLength;
                ulong requiredCount = ((requiredBitCount - uselessBitCount) + (N - 1)) / N;
                var newLength = requiredCount;
                return new ulong[newLength];
            }
            static IEnumerable<(ulong SourceStartBitIndex, ulong DestBitIndex, ulong Length, bool? Value)> GetRanges(IEnumerable<ulong> sortedSourceBitIndices, bool[] values, ulong bitCount)
            {
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

            static void SetBit(ulong[] dest, ulong bitIndex, bool value)
            {
                ulong flag = 1UL << (int)(bitIndex % N);
                if (value)
                    dest[bitIndex / N] |= flag;
                else
                    dest[bitIndex / N] &= ~flag;
            }
        }
        /// <summary>
        /// Creates a copy of <paramref name="source"/> and inserts a range of <paramref name="insertionBits"/>.
        /// </summary>
        /// <param name="source">The sequence to clone and insert into.</param>
        /// <param name="insertionBits">The container of the sequence to insert.</param>
        /// <param name="index">The bit index in <paramref name="source"/> to start inserting.</param>
        /// <param name="startIndex">The index in <paramref name="insertionBits"/> of the first bit to insert. </param>
        /// <param name="length">The number of bits from <paramref name="insertionBits"/> to insert.</param>
        /// <param name="sourceLength">The number of bits in <paramref name="source"/> that are relevant, i.e. have to be part of the result.</param>
        public static ulong[] InsertBits(this ulong[] source, ulong[] insertionBits, ulong index, ulong startIndex, ulong length, ulong? sourceLength)
        {
            sourceLength ??= (ulong)source.Length * 64UL;
            ulong requiredBitLength = sourceLength.Value + length;
            var result = new ulong[requiredBitLength.RoundUpToNearestMultipleOf(64) / 64];

            source.CopyBitsTo(result, 0, 0, startIndex);
            insertionBits.CopyBitsTo(result, startIndex, index, length);
            source.CopyBitsTo(result, startIndex, startIndex + length, requiredBitLength - length - index);

            return result;
        }
        public static void CopyBitsTo(this ulong[] source, ulong[] dest, ulong sourceStart, ulong destStart, ulong length)
        {
            const int N = 64;
            if (sourceStart + length > (ulong)source.Length * N) throw new ArgumentOutOfRangeException(nameof(sourceStart));
            if (destStart + length > (ulong)dest.Length * N) throw new ArgumentOutOfRangeException(nameof(destStart));
            if (length > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(length));

            ulong currentSource = sourceStart;
            ulong currentDest = destStart;
            long remaining = (long)length;
            while (remaining > 0)
            {
                ulong nextDestUlongBoundary = currentDest + (N - (currentDest % N));
                if (nextDestUlongBoundary < currentDest) throw new Exception();
                int sourceIndex = (int)(currentSource / N);
                ulong source1 = source[sourceIndex];
                ulong source2 = sourceIndex + 1 == source.Length ? 0UL : source[sourceIndex + 1];
                uint diff = (uint)(nextDestUlongBoundary - currentDest); // Math.Min((ulong)remaining, nextDestUlongBoundary - currentDest);
                ref var dst = ref dest[(int)(currentDest / N)];
                Copy(source1, source2, ref dst, (int)(currentSource % N), (int)(currentDest % N), Math.Min((uint)remaining, diff));

                remaining -= diff;
                currentDest += diff;
                currentSource += diff;
            }


            static void Copy(ulong source1, ulong source2, ref ulong dest, int index, int destIndex, uint length)
            {
                ulong orig1 = source1;
                ulong orig2 = source2;
                if (index < 0 || index >= N) throw new ArgumentOutOfRangeException(nameof(index));
                if (length < 0 || length > int.MaxValue || length > 2 * N) throw new ArgumentOutOfRangeException(nameof(length));
                if (destIndex + length > N) throw new ArgumentOutOfRangeException(nameof(destIndex));
                if (index + length > 2 * N) throw new ArgumentOutOfRangeException(nameof(index));

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
        static uint ClearLowBits(uint bits, int numberOfBits)
        {
            uint mask = uint.MaxValue << numberOfBits;
            return bits & mask;
        }
        static ulong ClearLowBits(ulong bits, int numberOfBits)
        {
            ulong mask = ulong.MaxValue << numberOfBits;
            return bits & mask;
        }
        static uint ClearHighBits(uint bits, int numberOfBitsToClear)
        {
            uint mask = uint.MaxValue >> numberOfBitsToClear;
            return bits & mask;
        }
        static ulong ClearHighBits(ulong bits, int numberOfBitsToClear)
        {
            ulong mask = ulong.MaxValue >> numberOfBitsToClear;
            return bits & mask;
        }
        /// <summary>
        /// Creates a ulong with bits set to one at indices [from, until).
        /// </summary>
        /// <param name="until">exclusive.</param>
        static ulong CreateULongMask(int from, int until)
        {
            if (from < 0 || from > 64) throw new ArgumentOutOfRangeException(nameof(from));
            if (until < from || until > 64) throw new ArgumentOutOfRangeException(nameof(until));

            return ClearHighBits(ClearLowBits(ulong.MaxValue, from), 64 - until);
        }
        static uint CreateUIntMask(int from, int until)
        {
            if (from < 0 || from > 32) throw new ArgumentOutOfRangeException(nameof(from));
            if (until < from || until > 32) throw new ArgumentOutOfRangeException(nameof(until));

            return ClearHighBits(ClearLowBits(uint.MaxValue, from), 32 - until);
        }
        /// <summary>
        /// Sets the bits from [0, until) and [from, 64] to zero.
        /// </summary>
        public static ulong Mask(this ulong value, int until, int from)
        {
            var mask = CreateULongMask(until, from);
            var result = value & mask;
            return result;
        }
        /// <summary>
        /// Sets the bits from [0, until) and [from, 32] to zero.
        /// </summary>
        public static uint Mask(this uint value, int until, int from)
        {
            var mask = CreateUIntMask(until, from);
            var result = value & mask;
            return result;
        }
        /// <summary>
        /// Removes bits at specified indices.
        /// </summary>
        /// <param name="bytes">The source sequence to remove from.</param>
        /// <param name="sortedBitIndices"> The indices of the bits to remove. </param>
        /// <param name="bytesLengthInBits"> The length of the bytes sequence. Defaults to <code>8 * bytes.Count()</code>.</param>
        public static byte[] RemoveBits(this IEnumerable<byte> bytes, ulong[] sortedBitIndices, int? bytesLengthInBits = null)
        {
            BitArray bitArray;
            if (bytesLengthInBits == null)
                bitArray = new BitArray(bytes);
            else
                bitArray = new BitArray(bytes, bytesLengthInBits.Value);
            bitArray.RemoveAt(sortedBitIndices);
            var result = new byte[checked((int)(bitArray.Length / 8 + ((bitArray.Length % 8) == 0 ? 0UL : 1)))];
            bitArray.CopyTo(result.AsSpan(), 0);
            return result;
        }

        /// <summary> Gets the bit index in the specified data of the specified item. </summary>
        public static long IndexOfBits(IEnumerable<ulong> data, ulong item, int? itemLength = null, ulong startIndex = 0, ulong? dataLength = null)
        {
            return IndexOfBits(data, new[] { item }, itemLength, startIndex, dataLength).BitIndex;
        }
        /// <summary> Gets the first bit index in the specified data of any of the specified equilong items. </summary>
        /// <param name="returnLastConsecutive"> When true, and when there's a match, the successive places are also checked and the last consecutive will be selected as the result.</param>
        /// <param name="dataLength">The exclusive bit index, after which <paramref name="data"/> is not considered caontaining values anymore. This is independent of <paramref name="startIndex"/>.</param>
        public static (long BitIndex, int ItemIndex) IndexOfBits(IEnumerable<ulong> data, IReadOnlyList<ulong> items, int? itemLength = null, ulong startIndex = 0, ulong? dataLength = null, bool returnLastConsecutive = false)
        {
            const int N = 64;
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (itemLength != null && (itemLength < 0 || itemLength > N)) throw new ArgumentOutOfRangeException(nameof(itemLength));
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            bool hasCount = data.TryGetNonEnumeratedCount(out int count);
            if (hasCount && startIndex > N * (ulong)count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            itemLength ??= N;
            dataLength ??= hasCount ? (ulong)(N * count) : null;

            if (items.Count == 0)
            {
                return (-1, -1);
            }

            long bitIndex = (long)startIndex;
            int elementIndex = (int)(startIndex / N);
            const int noMatch = -1;
            int matchedItemIndex = noMatch;
            foreach (var ((element, nextElement), isLast) in data.Skip(elementIndex).Append(0UL).Windowed2().WithIsLast())
            {
                while (Fits(bitIndex, elementIndex, itemLength.Value, dataLength, isLast))
                {
                    for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                    {
                        if (IsMatch(bitIndex, element, nextElement, elementIndex, items[itemIndex], itemLength.Value))
                        {
                            // at this point, a -1 return is impossible
                            if (returnLastConsecutive)
                            {
                                matchedItemIndex = itemIndex;
                                // only the currently matched item will be considered in getting the last consecutive
                                if (items.Count != 1)
                                    items = new[] { items[itemIndex] };
                            }
                            else
                                return (bitIndex, itemIndex);
                        }
                        else if (matchedItemIndex != noMatch)
                            return (bitIndex - 1, itemIndex);
                    }
                    bitIndex++;
                }
                elementIndex++;
            }
            if (matchedItemIndex != noMatch)
                return (bitIndex - 1, matchedItemIndex);
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

            if (start == end)
            {
                return 0;
            }

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
        [DebuggerHidden]
        internal static ulong ToULong(this int i)
        {
            if (i < 0) throw new ArgumentOutOfRangeException(nameof(i));
            return (ulong)i;
        }

        public static Half BitsAsHalf(this short s)
        {
            unsafe
            {
                short* pointer = &s;
                Half* halfPointer = (Half*)pointer;
                Half result = *halfPointer;
                return result;
            }
        }
        public static float BitsAsSingle(this int i)
        {
            unsafe
            {
                int* pointer = &i;
                float* floatPointer = (float*)pointer;
                float result = *floatPointer;
                return result;
            }
        }
        public static double BitsAsDouble(this long i)
        {
            unsafe
            {
                long* pointer = &i;
                double* doublePointer = (double*)pointer;
                double result = *doublePointer;
                return result;
            }
        }
        public static double BitsAsDouble(this ulong i)
        {
            unsafe
            {
                ulong* pointer = &i;
                double* doublePointer = (double*)pointer;
                double result = *doublePointer;
                return result;
            }
        }


        /// <summary>
        /// Gets whether a bitsequence in two ulong arrays are equal.
        /// </summary>
        /// <param name="length">The number of bits to compare for equality.</param>
        /// <param name="sourceBitEnd"> Index of last bit in <paramref name="source"/> that's still considered part of the sequence. </param>
        /// <param name="otherBitEnd"> Index of last bit in <paramref name="other"/> that's still considered part of the sequence. </param>
        public static bool BitSequenceEqual(this ulong[] source, ulong[] other, ulong sourceStartBitIndex, ulong otherStartBitIndex, ulong length, ulong? sourceBitEnd = null, ulong? otherBitEnd = null)
        {
            if (sourceBitEnd != null && sourceBitEnd > (ulong)source.Length * 64)
                throw new ArgumentOutOfRangeException(nameof(sourceBitEnd));
            if (otherBitEnd != null && otherBitEnd > (ulong)other.Length * 64)
                throw new ArgumentOutOfRangeException(nameof(otherBitEnd));
            if (sourceStartBitIndex + length > (sourceBitEnd ?? (ulong)source.Length * 64))
                return false;
            if (otherStartBitIndex + length > (otherBitEnd ?? (ulong)other.Length * 64))
                return false;
            if (length == 0)
                return true;
            sourceBitEnd = otherBitEnd = null; // doesn't make sense to use except for the range checks above

            checked
            {
                ulong sourceUlongwiseComparisonStartIndex = sourceStartBitIndex.RoundUpToNearestMultipleOf(64UL);
                ulong sourceUlongwiseComparisonEndIndex = (sourceStartBitIndex + length).RoundDownToNearestMultipleOf(64UL);
                ulong extraLengthBefore = Math.Min(sourceUlongwiseComparisonStartIndex - sourceStartBitIndex, length);
                ulong extraLengthAfter = sourceStartBitIndex + length - sourceUlongwiseComparisonEndIndex;
                ulong otherUlongwiseComparisonStartIndex = otherStartBitIndex + extraLengthBefore;
                ulong otherUlongwiseComparisonEndIndex;

                // compare bits before comparison full ulongs:
                var sourceBitsBefore = GetBits(source, sourceStartBitIndex, extraLengthBefore);
                var otherBitsBefore = GetBits(other, otherStartBitIndex, extraLengthBefore);
                if (sourceBitsBefore != otherBitsBefore)
                    return false;
                if (sourceUlongwiseComparisonStartIndex > sourceUlongwiseComparisonEndIndex)
                    return true;
                else
                    otherUlongwiseComparisonEndIndex = otherUlongwiseComparisonStartIndex + (sourceUlongwiseComparisonEndIndex - sourceUlongwiseComparisonStartIndex);

                ulong otherBitIndex = otherUlongwiseComparisonStartIndex;
                for (ulong sourceBitIndex = sourceUlongwiseComparisonStartIndex; sourceBitIndex < sourceUlongwiseComparisonEndIndex; sourceBitIndex += 64)
                {
                    ulong sourceUlong = source[sourceBitIndex / 64];
                    ulong otherUlong = GetBits(other, otherBitIndex, 64);

                    if (sourceUlong != otherUlong)
                        return false;

                    otherBitIndex += 64;
                }

                
                var sourceBitsAfter = GetBits(source, sourceUlongwiseComparisonEndIndex, extraLengthAfter);
                var otherBitsAfter = GetBits(other, otherUlongwiseComparisonEndIndex, extraLengthAfter);

                return sourceBitsAfter == otherBitsAfter;
            }

        }
        internal static ulong GetBits(ulong[] array, ulong bitIndex, ulong bitCount)
        {
            return TakeBits(first: array[bitIndex / 64],
                            second: (int)(bitIndex / 64) + 1 >= array.Length ? 0 : array[(bitIndex / 64) + 1],
                            start: (int)(bitIndex % 64),
                            end: (int)(bitIndex % 64) + checked((int)bitCount));
        }
        /// <summary>
        /// Gets the bits in the specified array up to the first ulong boundary. If <paramref name="bitIndex"/> is at a ulong boundary, returns 0.
        /// </summary>
        internal static ulong GetBitsFrom(ulong[] array, ulong bitIndex)
        {
            var boundary = bitIndex.RoundUpToNearestMultipleOf(64);
            return GetBits(array, bitIndex, boundary - bitIndex);
        }
        /// <summary>
        /// Gets the bits in the specified array from the nearest ulong boundary before <paramref name="bitIndex"/> to <paramref name="bitIndex"/>. If <paramref name="bitIndex"/> is at a ulong boundary, returns 0.
        /// </summary>
        internal static ulong GetBitsTo(ulong[] array, ulong bitIndex)
        {
            var boundary = bitIndex.RoundDownToNearestMultipleOf(64);
            return GetBits(array, boundary, bitIndex - boundary);
        }

        public static string FormatAsBits(this ulong bits, int digits = 64)
        {
            if (digits < 0 || digits > 64) throw new ArgumentOutOfRangeException(nameof(digits));

            var builder = new StringBuilder();
            builder.AppendAsBits(bits, digits);
            return builder.ToString();
        }
        /// <summary>
        /// Format to bits.
        /// <seealso cref="https://stackoverflow.com/a/38422245/308451"/>
        /// </summary>
        internal static string AppendAsBits(this StringBuilder builder, ulong bits, int digits)
        {
            ulong mask = 1UL << digits - 1;
            for (int i = 0; i < digits; i++)
            {
                builder.Append((bits & mask) != 0 ? '1' : '0');
                mask >>= 1;
                if ((i % 8) == 7 && i != digits - 1)
                {
                    builder.Append('_');
                }
            }
            return builder.ToString();
        }
        [DebuggerHidden]
        public static string FormatAsBits(this ulong[] bits, ulong? digits = null)
        {
            return bits.FormatAsBits(digits == null ? null : (int)digits);
        }
        private const char ULONG_SEPARATOR = '+';
        public static string FormatAsBits(this ulong[] bits, int? digits = null)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            if (digits != null && digits < 0) throw new ArgumentOutOfRangeException(nameof(digits));
            if (digits != null && digits > bits.Length * 64) throw new ArgumentOutOfRangeException(nameof(digits));

            var builder = new StringBuilder();

            int ulongCount = digits == null ? bits.Length : digits.Value / 64;
            int extraDigits = digits == null ? 0 : digits.Value % 64;
            if (extraDigits != 0)
            {
                int nonFirstBlockDigits = extraDigits - (extraDigits % 8);
                if (extraDigits % 8 != 0)
                {
                    builder.AppendAsBits(bits[ulongCount] >> nonFirstBlockDigits, extraDigits % 8);
                    if (nonFirstBlockDigits != 0)
                    {
                        builder.Append('_');
                    }
                }

                builder.AppendAsBits(bits[ulongCount], nonFirstBlockDigits);
                if (ulongCount != 0)
                {
                    builder.Append(ULONG_SEPARATOR);
                }
            }

            foreach (var (@ulong, isLast) in bits.Take(ulongCount).Reverse().WithIsLast())
            {
                builder.AppendAsBits(@ulong, 64);
                if (!isLast)
                {
                    builder.Append(ULONG_SEPARATOR);
                }
            }


            return builder.ToString();
        }
        public static string FormatAsBits(this ulong[] bits, ulong startIndex, ulong length)
        {
            // PERF
            var range = new Range(checked((int)startIndex), checked((int)(startIndex + length)));
            var segment = new BitArray(bits, bits.Length * 64)[range];
            var builder = new StringBuilder();
            foreach (var (bit, bitIndex) in segment.WithIndex().Reverse())
            {
                builder.Append(bit ? 1 : 0);
                if (bitIndex != 0)
                {
                    if ((bitIndex % 64) == 0)
                    {
                        builder.Append(ULONG_SEPARATOR);
                    }
                    else if ((bitIndex % 8) == 0)
                    {
                        builder.Append('_');
                    }
                }
            }
            return builder.ToString();
        }
        /// <summary>
        /// Gets the index of the bit after the highest nonzero bit.
        /// </summary>
        public static int CountBits(this ulong bits)
        {
            int i = 0;
            while (bits > 0)
            {
                bits >>= 1;
                i++;
            }
            return i;
        }
    }
}
