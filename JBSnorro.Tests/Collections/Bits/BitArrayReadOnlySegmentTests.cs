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
}
