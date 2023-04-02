using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
    [TestClass]
    public class BitArrayTests
    {
        [TestMethod]
        public void BitArrayCtorTest()
        {
            BitArray testObject = new BitArray(new bool[] { true, false, true, true, false, false, false, true });

            Contract.Assert(testObject.Length == 8);
            Contract.Assert(testObject[0]);
            Contract.Assert(!testObject[1]);
            Contract.Assert(testObject[2]);
            Contract.Assert(testObject[3]);
            Contract.Assert(!testObject[4]);
            Contract.Assert(!testObject[5]);
            Contract.Assert(!testObject[6]);
            Contract.Assert(testObject[7]);
        }
    }

    [TestClass]
    public class BitInsertionTests
    {
        const ulong highestBitSet = 1UL << 63;
        const ulong secondHighestBitSet = 1UL << 62;
        [TestMethod]
        public void NoBitInsertion()
        {
            var data = Array.Empty<ulong>();
            var result = data.InsertBits(Array.Empty<ulong>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);

            data = new ulong[] { 1 };
            result = data.InsertBits(Array.Empty<ulong>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);

            data = new ulong[] { 1, 255 };
            result = data.InsertBits(Array.Empty<ulong>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);
        }
        [TestMethod]
        public void SimpleBitInsertionIntoEmptyList()
        {
            var data = Array.Empty<ulong>();
            var result = data.InsertBits(new[] { 0UL }, new[] { false });
            Contract.AssertSequenceEqual(result, new ulong[] { 0 });

            result = data.InsertBits(new[] { 0UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 1 });
        }
        [TestMethod, ExpectedException(typeof(IndexOutOfRangeException))]
        public void SimpleBitInsertionIntoEmptyListGoesOutOfIndex()
        {
            var data = Array.Empty<ulong>();
            data.InsertBits(new[] { 1UL }, new[] { false });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingList()
        {
            var data = new ulong[] { 0b_0001_0010 };
            var result = data.InsertBits(new[] { 0UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0010_0101, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoMiddleOfFirstByte()
        {
            var data = new ulong[] { 0b_0000_0000 };
            var result = data.InsertBits(new[] { 1UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0000_0010, 0 });
        }
        [TestMethod]
        public void SimpleAppendBit()
        {
            var data = new ulong[] { 0b_0001_0010 };
            var result = data.InsertBits(new[] { 64UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0001_0010, 0b1 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingListWithCrossoverBit()
        {
            var data = new ulong[] { highestBitSet };
            var result = data.InsertBits(new[] { 0UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 1, 1 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingListWithCrossoverBits()
        {
            var data = new ulong[] { highestBitSet | 0b0001 };
            var result = data.InsertBits(new[] { 0UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0011, 1 });
        }
        [TestMethod]
        public void SimpleBitInsertionWithOnesInNextByte()
        {
            var data = new ulong[] { 0b_1110_0001, 0b_0000_0001 };
            var result = data.InsertBits(new[] { 0UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0001_1100_0011, 0b_0000_0010, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionInMiddleOfFlags()
        {
            var data = new ulong[] { highestBitSet | secondHighestBitSet, 0b_0000_0001 };
            var result = data.InsertBits(new[] { 63UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { highestBitSet | secondHighestBitSet, 0b_00011, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionInThirdElement()
        {
            var data = new ulong[] { 0, 0, 0 };
            var result = data.InsertBits(new[] { 128UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0, 0, 1, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionAtTheEndOfLastElement()
        {
            var data = new ulong[] { 0, 0, 0 };
            var result = data.InsertBits(new[] { 64 * 3UL }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0, 0, 0, 1 });
        }
        [TestMethod]
        public void SimpleBitInsertionAtTheEndOfNotFullElement()
        {
            var data = new ulong[] { 0b_00 };
            var result = data.InsertBits(new[] { 2UL }, new[] { true }, sourceLengthInBits: 2);
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_100 });
        }

        [TestMethod]
        public void InsertionsIn2Bytes()
        {
            // now both in one go:
            var input = new ulong[] { 0, 0b1111_1111 };
            var combined = input.InsertBits(new[] { 4UL, 64UL }, new[] { true, true });
            Contract.AssertSequenceEqual(combined, new ulong[] { 0b_0001_0000, 0b0011_1111_1110, 0 });
        }
        [TestMethod]
        public void InsertionsOf0In2Bytes()
        {
            // now both in one go:
            var input = new ulong[] { 0, 0b1111_1111 };
            var combined = input.InsertBits(new[] { 4UL, 64UL }, new[] { true, true });
            Contract.AssertSequenceEqual(combined, new ulong[] { 0b_0001_0000, 0b0011_1111_1110, 0 });
        }


        [TestMethod]
        public void InsertionsInSameBytes()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4UL, 6UL }, new[] { true, true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b1001_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsInSameBytesCloserTogether()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4UL, 5UL }, new[] { true, true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0101_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsAtSameSpot()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4UL, 4UL }, new[] { true, true });
            // in light of the two tests above, this must be the result, in order to remain consistent
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0011_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsInManyBytes()
        {
            var data = new ulong[] { 0b1111_1111, 0b0111_1111 }; // | highestBitSet | secondHighestBitSet};
            var result = data.InsertBits(new[] { 4UL, 64UL + 7 }, new[] { false, false });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0001_1110_1111, 0b1111_1110, 0 });
        }

        [TestMethod]
        public void TestInsertSegmentDoesntEnlargeCapacityIfUnneeded()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            var data = new BitArray(new ulong[] { item }, 30);
            var segment = data[0..10]; // = 11110000_10

            data.Insert(segment, 0);

            Contract.Assert(data.Capacity == 64);
        }
        [TestMethod]
        public void TestInsertionMaintainsContinuityOfBitsOverULongs()
        {
            const ulong item = 0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111UL;
            var data = new BitArray(new ulong[] { item }, 64);

            data.Insert(new BitArray(new bool[] { false })[Range.All], 0);

            Contract.Assert(data.ToString() == "1+11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111110");

            data.Insert(new BitArray(new bool[] { false })[Range.All], 0);
            Contract.Assert(data.ToString() == "11+11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111100");
        }

        [TestMethod]
        public void TestGettingRangeMaintainsContinuityOfBitsOverULongs()
        {
            const ulong item = 0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11110000UL;
            var data = new BitArray(new ulong[] { item }, 64);

            // Act
            var result = data[0..8];

            Contract.Assert(result.ToString() == "11110000");
        }
        [TestMethod]
        public void TestInsertSegment2()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            var data = new BitArray(new ulong[] { item }, 64);
            BitArrayReadOnlySegment segment = data[0..10]; // = 00_11110011

            data.Insert(segment, 0);

            var expected = new BitArray(new[] { 0b11_11000010UL, 0b10101001_01010111_11111111_11000000_00000000_00000011_11001100_11110011UL }.Reverse().ToArray(), 74);
            Contract.Assert(data.Capacity == 128);
            Contract.Assert(data.Equals(expected));
        }
    }

    [TestClass]
    public class BitRemovalTests
    {
        [TestMethod]
        public void Test_SimpleRemoval()
        {
            var array = new BitArray(new byte[] { 255 });
            array.RemoveAt(3);
            Contract.AssertSequenceEqual(array, Enumerable.Range(0, 7).Select(_ => true));
        }
        [TestMethod]
        public void Test_SimpleRemovalOnlyBit()
        {
            var array = new BitArray(new bool[] { true });
            array.RemoveAt(0);
            Contract.AssertSequenceEqual(array, Array.Empty<bool>());
        }
        [TestMethod]
        public void Test_SimpleRemovalAtBeginning()
        {
            var array = new BitArray(new bool[] { true, false, true });
            array.RemoveAt(0);
            Contract.AssertSequenceEqual(array, new bool[] { false, true });
        }
        [TestMethod]
        public void Test_SimpleRemovalAtEnd()
        {
            var array = new BitArray(new bool[] { true, false, true });
            array.RemoveAt(2);
            Contract.AssertSequenceEqual(array, new bool[] { true, false });
        }
        [TestMethod]
        public void Test_RemovalInSecondULong()
        {
            var array = new BitArray(Enumerable.Range(0, 50).SelectMany(_ => new bool[] { true, false }));
            array.RemoveAt(80); // i.e. longer than bits in a ulong
            var expected = Enumerable.Range(0, 50).SelectMany(_ => new bool[] { true, false }).ExceptAt(80);
            Contract.AssertSequenceEqual(array, expected);
        }
        [TestMethod]
        public void Test_RemovalofMultipleInULong()
        {
            var array = new BitArray(Enumerable.Range(0, 50).SelectMany(_ => new bool[] { true, false }));
            array.RemoveAt(40, 80, 81); // i.e. longer than bits in a ulong
            var expected = Enumerable.Range(0, 50).SelectMany(_ => new bool[] { true, false }).ExceptAt(40, 80, 81);
            Contract.AssertSequenceEqual(array, expected);
        }
        [TestMethod]
        public void Test_SimpleRemovalofMultiple()
        {
            var array = new BitArray(new bool[] { true, false, true, true, false, false, true });
            array.RemoveAt(4, 5);
            var expected = new bool[] { true, false, true, true, true };
            Contract.AssertSequenceEqual(array, expected);
        }
    }
    [TestClass]
    public class TestCopyBitArray
    {
        [TestMethod]
        public void CanCopyEmpty()
        {
            var src = new BitArray(Array.Empty<bool>());
            var dest = new byte[0];

            src.CopyTo(dest.AsSpan(), 0);

            Contract.AssertSequenceEqual(dest, Array.Empty<byte>());
        }
        [TestMethod]
        public void CanCopyEmptyToNonEmpty()
        {
            var src = new BitArray(Array.Empty<bool>());
            var dest = new byte[] { 255 };

            src.CopyTo(dest.AsSpan(), 0);

            Contract.AssertSequenceEqual(dest, new byte[] { 255 });
        }
        [TestMethod, ExpectedException(typeof(IndexOutOfRangeException))]
        public void CannotCopyNonEmptyToEmpty()
        {
            var src = new BitArray(new bool[] { false });
            var dest = Array.Empty<byte>();

            src.CopyTo(dest.AsSpan(), 0);
        }
        [TestMethod]
        public void CopySingleByte()
        {
            var src = new BitArray(new bool[] { true, false, true, false, true, false, true, false });
            var dest = new byte[1];

            src.CopyTo(dest.AsSpan(), 0);

            Contract.AssertSequenceEqual(dest, new byte[] { 0b_0101_0101 });
        }
        [TestMethod]
        public void CopyToOffset()
        {
            var src = new BitArray(new bool[] { true, false, true, false, true, false, true, false });
            var dest = new byte[2];

            src.CopyTo(dest.AsSpan(), 1);

            Contract.AssertSequenceEqual(dest, new byte[] { 0, 0b_0101_0101 });
        }
        [TestMethod]
        public void CopyMultipleBytes()
        {
            var src = new BitArray(new bool[] { true, false, true, false, true, false, true, false, true, true, true, true, false, false, false, false });
            var dest = new byte[2];

            src.CopyTo(dest.AsSpan(), 0);

            Contract.AssertSequenceEqual(dest, new byte[] { 0b_0101_0101, 0b_0000_1111 });
        }
    }
    [TestClass]
    public class BitArrayIndexOfTests
    {
        [TestMethod]
        public void TestFindExactUlongMatch()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            var array = new BitArray(new[] { item }, 64);
            var i = array.IndexOf(item);

            Contract.Assert(i == 0);
        }
        [TestMethod]
        public void TestFindMatchOnSecondBit()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            var array = new BitArray(new[] { item << 1, 1UL }, 128);
            var i = array.IndexOf(item);

            Contract.Assert(i == 1);

            // double check that when clearing last bit it doesn't work:
            array = new BitArray(new[] { item << 1, 0UL }, 128);
            i = array.IndexOf(item);

            Contract.Assert(i == -1);
        }


        [TestMethod]
        public void TestFindMatchOnLastNonAlignedBit()
        {
            const ulong item = 0b0010_0000_0000;
            var array = new BitArray(new[] { 0UL, item }, 74);
            Contract.Assert(new BitArray(new[] { 0UL, item }, 73).IndexOf(1, itemLength: 1) == -1);
            Contract.Assert(new BitArray(new[] { 0UL, item }, 74).IndexOf(1, itemLength: 1) == 73);
            Contract.Assert(new BitArray(new[] { 0UL, item }, 75).IndexOf(1, itemLength: 1) == 73);
        }
        [TestMethod]
        public void TestFindExactSecondUlongMatch()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            var array = new BitArray(new[] { 0UL, item }, 128);
            var i = array.IndexOf(item);

            Contract.Assert(i == 64);
        }
        [TestMethod]
        public void TestFindZero()
        {
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 4).IndexOf(0, itemLength: 1) == -1);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 5).IndexOf(0, itemLength: 1) == 4);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 6).IndexOf(0, itemLength: 1) == 4);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 4).IndexOf(0, itemLength: 2) == -1);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 5).IndexOf(0, itemLength: 2) == -1);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 6).IndexOf(0, itemLength: 2) == 4);
            Contract.Assert(new BitArray(new[] { 0b00001111UL }, 7).IndexOf(0, itemLength: 2) == 4);
        }

        [TestMethod]
        public void TestFindMultiple()
        {
            var t1 = AssertEquivalent(new[] { 0b00001111UL }, new[] { 0b0001UL, 0b0011UL }, 4);
            Contract.Assert(t1 == (2, 1));


            static (long, int) AssertEquivalent(ulong[] data, IReadOnlyList<ulong> items, int itemLength)
            {
                var bitArray = new BitArray(data, data.Length * 64);
                var result = bitArray.IndexOfAny(items, itemLength);

                var expected = equivalent(bitArray, items, itemLength);
                Contract.Assert(expected == result);

                return result;

                // a dumb equivalent implementation of IndexOfAny:
                static (long, int) equivalent(BitArray data, IReadOnlyList<ulong> items, int itemLength)
                {
                    return items.Select((item, i) => (data.IndexOf(item, itemLength), i))
                                .Where(pair => pair.Item1 != -1)
                                .MinBy(pair => pair.Item1 * 1000 + pair.Item2);
                }

            }
        }
    }
    [TestClass]
    public class BitArrayEqualityTests
    {
        [TestMethod]
        public void EmptyEqualsEmpty()
        {
            Contract.Assert(new BitArray().Equals(new BitArray()));
            Contract.Assert(new BitArray(length: 1).Equals(new BitArray(length: 1)));
            Contract.Assert(!new BitArray(length: 1).Equals(new BitArray(length: 2)));
        }
        [TestMethod]
        public void SimpleEqualityTests()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            Contract.Assert(new BitArray(new ulong[] { item, item.Mask(0, 44) }, 128).Equals(new BitArray(new[] { item, item.Mask(0, 44) }, 128)));
            Contract.Assert(new BitArray(new ulong[] { item, item.Mask(0, 44) }, 100).Equals(new BitArray(new[] { item, item.Mask(0, 44) }, 100)));
            Contract.Assert(new BitArray(new ulong[] { item, item.Mask(0, 44) }, 65).Equals(new BitArray(new[] { item, item.Mask(0, 44) }, 65)));
            Contract.Assert(new BitArray(new ulong[] { item.Mask(0, 20), item.Mask(0, 44) }, 128).Equals(new BitArray(new[] { item.Mask(0, 20), item.Mask(0, 44) }, 128)));
        }
        [TestMethod]
        public void TestBitArrayEqualityToSegment()
        {
            const ulong item1 = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            const ulong item2 = 0b11000000_11101010_01010101_10111111_11110000_00010000_01100000_00000000UL;
            var array = new BitArray(new ulong[] { item1, item2 }, 120);
            var clone = array.Clone();
            clone.Insert(0, false);
            var segment = clone[1..(1 + (int)array.Length)];

            // Act
            bool equals = array.Equals(segment);
            bool equals2 = segment.Equals(array);

            Contract.Assert(equals);
            Contract.Assert(equals2);
        }
        [TestMethod]
        public void TestDualULongSegmentEqualityToSegment()
        {
            const ulong item1 = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            const ulong item2 = 0b11000000_11101010_01010101_10111111_11110000_00010000_01100000_00000000UL;
            var array = new BitArray(new ulong[] { item1, item2 }, 120);
            var secondClone = array.Clone();
            secondClone.Insert(0, false);
            secondClone.Insert(0, false);
            var secondSegment = secondClone[2..(2 + (int)array.Length)];

            // Act
            bool equals = array.Equals(secondSegment);

            Contract.Assert(equals);
        }
        [TestMethod]
        public void TestMultiULongSegmentEqualityToSegment()
        {
            var (array, shiftedSegment) = CreateShifted(numberOfUlongs: 4, bitLength: 120, shift: 2, seed: 50);

            // Act
            bool equals = array.Equals(shiftedSegment);

            Contract.Assert(equals);
        }

        public static (BitArray Array, BitArrayReadOnlySegment Shifted) CreateShifted(ulong numberOfUlongs, ulong bitLength, int shift, int seed)
        {
            var r = new Random(seed);

            var data = new ulong[numberOfUlongs];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (ulong)r.NextInt64();
            }
            var array = new BitArray(data, bitLength);
            var clone = array.Clone();
            for (int i = 0; i < shift; i++)
            {
                clone.Insert(0, false);

            }
            var segment = clone[2..(2 + (int)array.Length)];
            return (array, segment);
        }
    }
    [TestClass]
    public class BitArrayCopyToTests
    {
        [TestMethod]
        public void TestCopyBits()
        {
            foreach (var (src, dst, expected, srcIndex, length, dstIndex) in new[] {
                (new BitArray(new[] { 0b00001111UL }, 4), new BitArray(length: 8), new BitArray(new[] { 0b00001111UL }, 8), 0UL, 4UL, 0UL),
                (new BitArray(new[] { 0b00001111UL }, 3), new BitArray(length: 8), new BitArray(new[] { 0b00000111UL }, 8), 0UL, 3UL, 0UL),
                (new BitArray(new[] { 0b00001111UL }, 3), new BitArray(length: 8), new BitArray(new[] { 0b00001110UL }, 8), 0UL, 3UL, 1UL),
                (new BitArray(new[] { 0b0001_0110UL }, 8), new BitArray(length: 128), new BitArray(new[] { 0UL, 0b0000_1011UL }, 128), 0UL, 8UL, 63UL),
                (new BitArray(new[] { 0b00010111UL }, 8), new BitArray(length: 128), new BitArray(new[] { 1UL << 63, 0b001011UL }, 128), 0UL, 8UL, 63UL),
            })
            {
                src.CopyTo(dst, srcIndex, length, dstIndex);

                Contract.Assert(dst.Equals(expected));
            }
        }
    }

    [TestClass]
    public class BitArrayShaTests
    {
        [TestMethod]
        public void TestArrayHasSameShaAsShiftedSegment()
        {
            for (ulong i = 0; i < 150; i += 10)
            {
                var (array, shifted) = BitArrayEqualityTests.CreateShifted(numberOfUlongs: 4, bitLength: i, shift: 2, seed: 50);

                var arraySha = array.ComputeSHA1();
                var shiftedSha = shifted.ComputeSHA1();

                Contract.Assert(arraySha == shiftedSha);
            }
        }
        [TestMethod]
        public void TestSubsegmentDoesntHaveSameShaAsShiftedArray()
        {
            for (ulong i = 11; i < 150; i += 10)
            {
                var (array, shifted) = BitArrayEqualityTests.CreateShifted(numberOfUlongs: 4, bitLength: i, shift: 2, seed: 50);

                var arraySha = array[0..10].ComputeSHA1();
                var shiftedSha = shifted.ComputeSHA1();

                Contract.Assert(arraySha != shiftedSha);
            }
        }
        [TestMethod]
        public void TestEmptyArrayHasSameShaAsEmptySegment()
        {
            var (array, shifted) = BitArrayEqualityTests.CreateShifted(numberOfUlongs: 4, bitLength: 0, shift: 5, seed: 10);

            var arraySha = array.ComputeSHA1();
            var shiftedSha = shifted.ComputeSHA1();

            Contract.Assert(arraySha == shiftedSha);
        }
    }
    [TestClass]
    public class BitArrayToStringTests
    {
        [TestMethod]
        public void TestFormatToBitsSingleUlong()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            string expected = "11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011";
            Contract.Assert(item.FormatAsBits() == expected);

            var array = new BitArray(new[] { item }, 64);
            Contract.Assert(array.ToString() == expected);
        }
        [TestMethod]
        public void TestFormatToBitsSubstring()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            string expected = "10101010_01010101_11111111_11110000_00000000_00000000_11110011";
            Contract.Assert(item.FormatAsBits(56) == expected);

            var array = new BitArray(new[] { item }, 56);
            Contract.Assert(array.ToString() == expected);
        }
        [TestMethod]
        public void TestFormatToBitsMulti()
        {
            const ulong item = 0b11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011UL;
            const ulong item2 = 0b11110000_11111111_00000000_00000000_11110000_10101010_01010101_11110011UL;
            const string EXPECTED = "11110000_11111111_00000000_00000000_11110000_10101010_01010101_11110011+11110000_10101010_01010101_11111111_11110000_00000000_00000000_11110011";

            foreach (bool withExtraUlong in new[] { false, true })
            {
                foreach (var length in new[] { 128, 120, 121, 119, 65, 64, 63 })
                {
                    int underscoresToRemoveCount = length switch { 65 => 7, 64 => 8, 63 => 8, <= 120 => 1, _ => 0 };
                    int digitsToRemoveCount = (128 - length);
                    var expected = EXPECTED[(digitsToRemoveCount + underscoresToRemoveCount)..];

                    // Act

                    var array = new BitArray(withExtraUlong ? new[] { item, item2, 0UL } : new[] { item, item2 }, length);

                    Contract.Assert(array.ToString() == expected);
                }
            }
        }
    }
}
