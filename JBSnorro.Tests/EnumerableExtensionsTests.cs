using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class EnumerableExtensionsTests
	{
		[TestMethod, ExpectedException(typeof(ArgumentException))]
		public void TestMaximumByThrowsOnEmpty()
		{
			new int[] { }.MaximumBy(_ => _);
		}
		[TestMethod]
		public void TestMaximumBy()
		{
			const int expected = 8;
			var result = new int[] { 5, 1, 6, 8, 1, 1, 4, 0, -10 }.MaximumBy(_ => _);

			Assert.AreEqual(expected, result);
		}
	}
}
