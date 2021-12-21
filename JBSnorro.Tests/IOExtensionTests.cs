using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests
{
	[TestClass]
	public class IsFullPathTests
	{
		[TestMethod]
		public void TestIsFullPath()
		{
			Assert.IsTrue(IOExtensions.IsFullPathInUnix("/home"));
			Assert.IsFalse(IOExtensions.IsFullPathInWindows("/home"));

			Assert.IsFalse(IOExtensions.IsFullPathInUnix("C:\\a"));
			Assert.IsTrue(IOExtensions.IsFullPathInWindows("C:\\a"));
		}
	}
}
