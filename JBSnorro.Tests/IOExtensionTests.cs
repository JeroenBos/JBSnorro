using Microsoft.VisualStudio.TestTools.UnitTesting;
using JBSnorro;

namespace Tests.JBSnorro;

[TestClass]
public class IsFullPathTests
{
    [TestMethod]
    public void TestIsFullPathInUnix()
    {
        Assert.IsTrue(IOExtensions.IsFullPathInUnix("/home"));
        Assert.IsFalse(IOExtensions.IsFullPathInUnix("C:\\a"));
    }
    [TestOnWindowsOnly]
    public void TestIsFullPathinWindows()
    {
        Assert.IsFalse(IOExtensions.IsFullPathInWindows("/home"));
        Assert.IsTrue(IOExtensions.IsFullPathInWindows("C:\\a"));
    }
}
