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
            var result = data.InsertBits(Array.Empty<int>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);

            data = new ulong[] { 1 };
            result = data.InsertBits(Array.Empty<int>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);

            data = new ulong[] { 1, 255 };
            result = data.InsertBits(Array.Empty<int>(), Array.Empty<bool>());
            Contract.AssertSequenceEqual(data, result);
        }
        [TestMethod]
        public void SimpleBitInsertionIntoEmptyList()
        {
            var data = Array.Empty<ulong>();
            var result = data.InsertBits(new[] { 0 }, new[] { false });
            Contract.AssertSequenceEqual(result, new ulong[] { 0 });

            result = data.InsertBits(new[] { 0 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 1 });
        }
        [TestMethod, ExpectedException(typeof(IndexOutOfRangeException))]
        public void SimpleBitInsertionIntoEmptyListGoesOutOfIndex()
        {
            var data = Array.Empty<ulong>();
            data.InsertBits(new[] { 1 }, new[] { false });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingList()
        {
            var data = new ulong[] { 0b_0001_0010 };
            var result = data.InsertBits(new[] { 0 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0010_0101, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoMiddleOfFirstByte()
        {
            var data = new ulong[] { 0b_0000_0000 };
            var result = data.InsertBits(new[] { 1 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0000_0010, 0 });
        }
        [TestMethod]
        public void SimpleAppendBit()
        {
            var data = new ulong[] { 0b_0001_0010 };
            var result = data.InsertBits(new[] { 64 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0001_0010, 0b1 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingListWithCrossoverBit()
        {
            var data = new ulong[] { highestBitSet };
            var result = data.InsertBits(new[] { 0 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 1, 1 });
        }
        [TestMethod]
        public void SimpleBitInsertionIntoExistingListWithCrossoverBits()
        {
            var data = new ulong[] { highestBitSet | 0b0001 };
            var result = data.InsertBits(new[] { 0 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0011, 1 });
        }
        [TestMethod]
        public void SimpleBitInsertionWithOnesInNextByte()
        {
            var data = new ulong[] { 0b_1110_0001, 0b_0000_0001 };
            var result = data.InsertBits(new[] { 0 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b_0001_1100_0011, 0b_0000_0010, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionInMiddleOfFlags()
        {
            var data = new ulong[] { highestBitSet | secondHighestBitSet, 0b_0000_0001 };
            var result = data.InsertBits(new[] { 63 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { highestBitSet | secondHighestBitSet, 0b_00011, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionInThirdElement()
        {
            var data = new ulong[] { 0, 0, 0 };
            var result = data.InsertBits(new[] { 128 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0, 0, 1, 0 });
        }
        [TestMethod]
        public void SimpleBitInsertionAtTheEndOfLastElement()
        {
            var data = new ulong[] { 0, 0, 0 };
            var result = data.InsertBits(new[] { 64 * 3 }, new[] { true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0, 0, 0, 1 });
        }
        [TestMethod]
        public void InsertionsIn2Bytes()
        {
            // now both in one go:
            var input = new ulong[] { 0, 0b1111_1111 };
            var combined = input.InsertBits(new[] { 4, 64 }, new[] { true, true });
            Contract.AssertSequenceEqual(combined, new ulong[] { 0b_0001_0000, 0b0011_1111_1110, 0 });
        }
        [TestMethod]
        public void InsertionsOf0In2Bytes()
        {
            // now both in one go:
            var input = new ulong[] { 0, 0b1111_1111 };
            var combined = input.InsertBits(new[] { 4, 64 }, new[] { true, true });
            Contract.AssertSequenceEqual(combined, new ulong[] { 0b_0001_0000, 0b0011_1111_1110, 0 });
        }


        [TestMethod]
        public void InsertionsInSameBytes()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4, 6 }, new[] { true, true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b1001_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsInSameBytesCloserTogether()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4, 5 }, new[] { true, true });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0101_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsAtSameSpot()
        {
            var data = new ulong[] { 0, 0b1111_1111 };
            var result = data.InsertBits(new[] { 4, 4 }, new[] { true, true });
            // in light of the two tests above, this must be the result, in order to remain consistent
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0011_0000, 0b0011_1111_1100, 0 });
        }
        [TestMethod]
        public void InsertionsInManyBytes()
        {
            var data = new ulong[] { 0b1111_1111, 0b0111_1111 }; // | highestBitSet | secondHighestBitSet};
            var result = data.InsertBits(new[] { 4, 64 + 7 }, new[] { false, false });
            Contract.AssertSequenceEqual(result, new ulong[] { 0b0001_1110_1111, 0b1111_1110, 0 });
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
}
