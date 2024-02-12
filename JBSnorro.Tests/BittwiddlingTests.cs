using JBSnorro;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace Tests.JBSnorro;

[TestClass]
public class BittwiddlingTests
{
    [TestMethod]
    public void TestXor()
    {
        var u = 1UL;
        var v = 2UL;

        Contract.Assert(u.Xor(0) == 0);
        Contract.Assert(u.Xor(1) == 3);
        Contract.Assert(v.Xor(0) == 3);
        Contract.Assert(v.Xor(1) == 0);
    }
    [TestMethod]
    public void Test_reverse_bits()
    {
        Assert.AreEqual(0b10100000_00000000_00000000_00000000, BitTwiddling.ReverseBitwise(0b101U));
    }
}
