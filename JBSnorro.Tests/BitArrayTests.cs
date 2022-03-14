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
