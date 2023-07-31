using JBSnorro;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro;

[TestClass]
public class ExtensionTests
{
	[TestMethod]
	public void TestSplitByIndices()
	{
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 2, 4, 7 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 0, 4, 7 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 0 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 0, 5 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 1, 5 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 1, 5, 6 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 1, 5, 5 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 1, 5, 5, 9 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 0, 5, 9 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 1, 9 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 0, 9 });
		TestSplitByindices(new int[] { 5, 1, 7, 10, 1, 2, 2, 4, 0, 8 }, new int[] { 9 });
		TestSplitByindices(new int[] { }, new int[] { });
		TestSplitByindices(new int[] { 1, 2, 3 }, new int[] { });
		TestSplitByindices(new int[] { 1, 2, 3 }, new int[] { 2 });
		TestSplitByindices(new int[] { }, new int[] { 0 });
	}

	/// <summary> Throws if the GeneralExtensions.SplitByIndices does not work for the specified test parameters. </summary>
	private static void TestSplitByindices<T>(IEnumerable<T> testSequence, IEnumerable<int> splitIndices)
	{
		var sortedSplitIndices = splitIndices.ToSortedList();
		if (!testSequence.SplitAtIndices(sortedSplitIndices)
						 .Concat()
						 .SequenceEqual(testSequence))
			throw new Exception();

		if (testSequence.SplitAtIndices(sortedSplitIndices).Count() != splitIndices.Count() + 1)
			throw new Exception();
	}
}
