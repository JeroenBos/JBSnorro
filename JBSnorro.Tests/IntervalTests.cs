using JBSnorro.Diagnostics;
using JBSnorro.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro;

[TestClass]
public class IntervalTests
{
	[TestMethod]
	public void TestContainment()
	{
		var a = new Interval(0, 3);
		var b = new Interval(-1, 3, endInclusive: true);

		Contract.Assert(a.OverlapsWith(b));
		Contract.Assert(!a.DisjointFrom(b));
		Contract.Assert(!a.Contains(b));
		Contract.Assert(b.Contains(a));
	}

	[TestMethod]
	public void TestEmptyInterval()
	{
		var empty = new Interval();
		var a = new Interval(-1, 3);

		Contract.Assert(empty.DisjointFrom(empty));
		Contract.Assert(!empty.OverlapsWith(empty));
		Contract.Assert(!empty.Contains(empty));
	}
	[TestMethod]
	public void TestEmptyWithOtherInterval()
	{
		var empty = new Interval();
		var other = new Interval(-1, 3);

		Contract.Assert(other.DisjointFrom(empty));
		Contract.Assert(!other.OverlapsWith(empty));
		Contract.Assert(other.Contains(empty));
		Contract.Assert(!empty.Contains(other));
	}
	[TestMethod]
	public void TestStartOpenClosedEquality()
	{
		var a = new Interval(1, 2);
		var b = new Interval(0, 2, false);

		Contract.Assert(a.Equals(b));
	}
	[TestMethod]
	public void TestEndOpenClosedEquality()
	{
		var a = new Interval(1, 2);
		var b = new Interval(1, 1, endInclusive: true);

		Contract.Assert(a.Equals(b));
	}
}
