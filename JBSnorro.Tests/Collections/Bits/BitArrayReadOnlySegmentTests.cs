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
    public void TestToString()
    {
        var ulong1 = 0b1_0000_1111UL;
        var ulong2 = 0b1_0101_0101UL;
        var array = new BitArray(new[] { ulong1, ulong2 }, length: 80);

        var actual = array.ToString();
        var expected = "00000001_01010101+00000000_00000000_00000000_00000000_00000000_00000000_00000001_00001111";

        Contract.Assert(actual == expected);
    }
}
