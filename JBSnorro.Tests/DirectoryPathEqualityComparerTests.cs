using Microsoft.VisualStudio.TestTools.UnitTesting;
using JBSnorro;

namespace Tests.JBSnorro;

[TestClass]
public class DirectoryPathEqualityComparerTests
{
	[TestMethod]
	public void TestEquality()
	{
		Assert.IsTrue(DirectoryPathEqualityComparer.Equals(@"D:\ASDE", @"D:\ASDE"));
	}
	[TestMethod]
	public void TestInequality()
	{
		Assert.IsFalse(DirectoryPathEqualityComparer.Equals(@"D:\ASDE", @"D:\"));
	}
	// [TestMethod, ExpectedException(typeof(ContractException))]
	// Uri.TryCreate considers "abc" a valid absolute Uri on linux. TODO: look into
	public void AssertDirectoryPathsMustBeAbsolute()
	{
		const string relativeDirectory = "abc";
		Assert.IsFalse(DirectoryPathEqualityComparer.Equals(relativeDirectory, @"D:\"));
	}
}
