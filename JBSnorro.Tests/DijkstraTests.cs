using JBSnorro;
using JBSnorro.Algorithms;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace Tests.JBSnorro;

[TestClass]
public class DijkstraTests
{
	[TestMethod]
	public void Test()
	{
		var path = Dijkstra<int>.FindPath(new[] { 0 }, i => new[] { i + 1 }, i => i == 10)!;

		Contract.Assert(path.SequenceEqual(Enumerable.Range(0, 10 + 1 - 0)));
	}
	[TestMethod]
	public void Test1()
	{
		var path = Dijkstra<int>.FindPath(new[] { 3 }, i => new[] { i + 1 }, i => i == 10)!;

		Contract.Assert(path.SequenceEqual(Enumerable.Range(3, 10 + 1 - 3)));
	}
	[TestMethod]
	public void Test2()
	{
		var path = Dijkstra<int>.FindPath(new[] { 1 }, i => new[] { i + 1, i * 2 }, i => i == 10)!;

		Contract.Assert(path.SequenceEqual(new[] { 1, 2, 4, 5, 10 }));
	}
	[TestMethod]
	public void Test4()
	{
		var path = Dijkstra<int>.FindPath(new[] { 1 }, i => new[] { i + 2, i * 3 - 1 }, i => i == 10)!;

		Contract.Assert(path.SequenceEqual(new[] { 1, 3, 8, 10 }));
	}

	[TestMethod]
	public void Test5()
	{
		var path = Dijkstra<int>.FindPath(new[] { 10 }, i => new[] { i + 2, i * 3 - 1 }, i => i == 10)!;

		Contract.Assert(path.SequenceEqual(new[] { 10 }));
	}

	[TestMethod]
	public void TestFail()
	{
		var path = Dijkstra<int>.FindPath(new[] { 1 }, i => new[] { (i * 3 - 1) % 20 }, i => i == 10)!;

		Contract.Assert(path == null);
	}

	[TestMethod]
	public void TestImplements()
	{
		Contract.Assert(typeof(IList).Implements(typeof(ICollection)));
		Contract.Assert(typeof(List<object>).Implements(typeof(ICollection)));
		Contract.Assert(typeof(List<object>).Implements(typeof(ICollection<object>)));
		Contract.Assert(!typeof(List<object>).Implements(typeof(ICollection<int>)));
		Contract.Assert(typeof(List<int>).Implements(typeof(ICollection<int>)));
		Contract.Assert(!typeof(List<int>).Implements(typeof(ICollection<object>)));
	}

	[TestMethod]
	public void HeapTest()
	{
		var heap = new Heap<int>(new int[] { 6, 1, 7, 3, 2, 5, 4 });

		List<int> sortedNumbers = new List<int>();
		while (heap.Count != 0)
			sortedNumbers.Add(heap.RemoveNext());

		Contract.Assert(sortedNumbers.IsSorted());
	}

	[TestMethod]
	public void HeapTestAddition()
	{
		var heap = new Heap<int>();

		heap.Add(6);
		heap.Add(1);
		heap.Add(3);
		heap.Add(7);
		heap.Add(2);
		heap.Add(5);
		heap.Add(4);

		List<int> sortedNumbers = new List<int>();
		while (heap.Count != 0)
			sortedNumbers.Add(heap.RemoveNext());

		Contract.Assert(sortedNumbers.IsSorted());
	}

	[TestMethod]
	public void HeapSpecificAdditionTest()
	{
		var heap = new Heap<int>();

		heap.Add(2);
		heap.Add(3);

		List<int> sortedNumbers = new List<int>();
		while (heap.Count != 0)
			sortedNumbers.Add(heap.RemoveNext());

		Contract.Assert(sortedNumbers.IsSorted());
	}
}
