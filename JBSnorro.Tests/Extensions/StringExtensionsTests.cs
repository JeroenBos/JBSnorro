using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    public void SubstringUntilLast_Returns_Input_If_Not_Found()
    {
        Assert.AreEqual("asdf", "asdf".SubstringUntilLast("t"));
    }
    [TestMethod]
    public void SubstringUntilLast_Returns_Until_Last_Input_If_Found()
    {
        Assert.AreEqual("abcd.efgh", "abcd.efgh.ijkl".SubstringUntilLast("."));
    }
    [TestMethod]
    public void SubstringUntilLast_Returns_Until_Last_Contiguous_Match()
    {
        Assert.AreEqual("AAAAAAAA", "AAAAAAAAAA".SubstringUntilLast("AA"));
    }
}
