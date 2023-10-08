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
    public void TestCountOnes()
    {
        var array = new BitArray(new[] { 0b110000UL, 0b1100UL }, 70);

        var expected = 4UL;
        var actual = array.CountOnes();

        Contract.Assert(expected == actual);
    }
}
