using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.TypeExtensions;

[TestClass]
public class IsOpenlyAssignableFromTests
{
	[TestMethod]
	public void IntIsAssignableToInt() => Contract.Assert(typeof(int).IsOpenlyAssignableFrom(typeof(int)));

	[TestMethod]
	public void ListOfIntIsAssignableToListOfInt() => Contract.Assert(typeof(List<int>).IsOpenlyAssignableFrom(typeof(List<int>)));
	[TestMethod]
	public void ListOfIntIsAssignableToOpenList() => Contract.Assert(typeof(List<>).IsOpenlyAssignableFrom(typeof(List<int>)));
	[TestMethod]
	public void OpenListIsNotAssignableToListOfInt() => Contract.Assert(!typeof(List<int>).IsOpenlyAssignableFrom(typeof(List<>)));

	[TestMethod]
	public void IListOfIntIsAssignableToIListOfInt() => Contract.Assert(typeof(IList<int>).IsOpenlyAssignableFrom(typeof(IList<int>)));
	[TestMethod]
	public void ListOfIntIsAssignableToOpenIList() => Contract.Assert(typeof(IList<>).IsOpenlyAssignableFrom(typeof(IList<int>)));
	[TestMethod]
	public void OpenIListIsNotAssignableToIListOfInt() => Contract.Assert(!typeof(IList<int>).IsOpenlyAssignableFrom(typeof(List<>)));
}
