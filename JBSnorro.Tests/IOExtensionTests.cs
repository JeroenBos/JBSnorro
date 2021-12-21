using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests
{
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
}
