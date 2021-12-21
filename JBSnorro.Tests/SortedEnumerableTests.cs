using JBSnorro.Collections.Sorted;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class SortedEnumerableTests
	{
		[TestMethod]
		public void SimpleTest()
		{
			var input = new SortedEnumerable<int>(new int[] { 1, 2, 3, 4 });
			var output = input.WhereSorted(i => i >= 2);

			Assert.AreEqual(3, output.Count());
		}
		[TestMethod]
		public void DerferredExecutionTest()
		{
			var input = new SortedList<int>(new List<int> { 1, 2 });
			var output = input.WhereSorted(i => i >= 2);
			input.Add(3);
			input.Add(4);

			Assert.AreEqual(3, output.Count());
		}
		[TestMethod]
		public void TestSimpleListAddition()
		{
			var list = new SortedList<int>(new List<int> { 0, 1, 3 });

			list.Add(2);

			Assert.AreEqual(2, list[2]);
		}
		[TestMethod]
		public void TestSimpleLinkedListAddition()
		{
			var list = new SortedLinkedList<int>();

			list.Add(0);
			list.Add(1);
			list.Add(3);
			list.Add(2);

			Assert.AreEqual(2, list.ElementAt(2));
		}
	}
}
