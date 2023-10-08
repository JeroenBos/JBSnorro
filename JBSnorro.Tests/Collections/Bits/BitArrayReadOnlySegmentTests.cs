using global::JBSnorro.Extensions;
using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Collections.Bits;

[TestClass]
public class BitArrayReadOnlySegmentTests
{
    [TestMethod]
    public void TestHashCode()
    {
        int count = 100;
        var hashCodes = new HashSet<int>();
        for (ulong bits = 0; bits < 100; bits++)
        {
            var segment = new BitArray(new ulong[] { bits }, 10)[..];
            hashCodes.Add(segment.GetHashCode());
        }

        Contract.Assert(hashCodes.Count == count);


        for (ulong bits = 0; bits < 100; bits++)
        {
            var segment = new BitArray(new ulong[] { bits }, 10)[..];
            hashCodes.Add(segment.GetHashCode());
        }
        Contract.Assert(hashCodes.Count == count);
    }
    [TestMethod]
    public void TestXor()
    {
        BitArray.ReverseToString = true;
        var ulong1 = 0b110000UL;
        var ulong2 = 0b100010UL;
        var expctd = 0b010010UL;
        const int length = 70;

        var array1 = new BitArray(new[] { ulong1, 0b1100UL }, length);
        var array2 = new BitArray(new[] { ulong2, 0b1010UL }, length);
        var expected = new BitArray(new[] { expctd, 0b0110UL }, length);

        var actual = array1.Clone();
        actual.Xor(array2);

        Contract.Assert(actual.Equals(expected));
    }
    [TestMethod]
    public void TestSettingBit()
    {
        var array = new BitArray(new bool[] { false });
        Contract.Assert(array[0] is false);

        array[0] = true;
        Contract.Assert(array[0] is true);

        array[0] = false;
        Contract.Assert(array[0] is false);
    }
    [TestMethod]
    public void TestSettingBitKeepsOthersIntact()
    {
        foreach (bool keepItAt in new bool[] { true, false })
        {
            var array = new BitArray(new bool[] { false, keepItAt });
            Contract.Assert(array[1] == keepItAt);

            array[0] = true;
            Contract.Assert(array[1] == keepItAt);

            array[0] = false;
            Contract.Assert(array[1] == keepItAt);
        }
    }
    [TestMethod]
    public void TestXorSegment()
    {
        BitArray.ReverseToString = true;
        var ulong1 = 0b0101UL;
        var ulong2 = 0b1001UL;
        var expctd = 0b1100UL;
        const int length = 70;

        var segment1 = new BitArray(new[] { ulong1, 0b1100UL }, length)[..];
        var segment2 = new BitArray(new[] { ulong2, 0b1010UL }, length)[..];
        var expected = new BitArray(new[] { expctd, 0b0110UL }, length);

        var actual = segment1.Xor(segment2);

        Contract.Assert(actual.Equals(expected));
    }
    [TestMethod]
    public void TestCountOnes()
    {
        var array = new BitArray(new[] { 0b110000UL, 0b1100UL }, 70);

        var expected = 4UL;
        var actual = array.CountOnes();

        Contract.Assert(expected == actual);
    }
}
