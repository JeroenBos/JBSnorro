using JBSnorro;
using JBSnorro.Algorithms;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using static JBSnorro.Combinatorics;

namespace Tests.JBSnorro;

public sealed class Filling : Combinatorics.IFillable
{
    public object? Tag { get; }
    public IReadOnlyList<bool> Occupation { get; }

    public Filling(object tag, IReadOnlyList<bool> usage)
    {
        Tag = tag;
        Occupation = usage;
    }
    public Filling(Gap gap, int length)
    {
        var occupation = new bool[length];
        for (int i = gap.Start; i < gap.End; i++)
        {
            occupation[i] = true;
        }
        Occupation = occupation;
    }
    public override int GetHashCode()
    {
        return Tag!.GetHashCode() + Occupation.GetHashCode();
    }
    public override bool Equals(object? obj)
    {
        return obj is Filling && Equals((Filling)obj);
    }
    public bool Equals(Filling other)
    {
        return other != null && other.Tag == this.Tag && other.Occupation.SequenceEqual(this.Occupation);
    }
    public override string ToString()
    {
        return "F" + Tag;
    }
}

[TestClass]
public class Tests
{
    [TestMethod]
    public void TestSortedRangeExcept()
    {
        var result = new int[0].RangeExcept(0, 0).ToArray();
        Contract.Requires(result.SequenceEqual(new int[0]));

        result = new int[0].RangeExcept(0, 1).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0 }));

        result = new[] { 0 }.RangeExcept(0, 1).ToArray();
        Contract.Requires(result.SequenceEqual(new int[0]));

        result = new int[0].RangeExcept(0, 2).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0, 1 }));

        result = new[] { 0 }.RangeExcept(0, 2).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 1 }));

        result = new[] { 1 }.RangeExcept(0, 2).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0 }));

        result = new[] { 0, 1 }.RangeExcept(0, 2).ToArray();
        Contract.Requires(result.SequenceEqual(new int[0]));

        result = new[] { 0, 1, 2, 4 }.RangeExcept(0, 5).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 3 }));

        result = new[] { 0, 2, 4 }.RangeExcept(0, 5).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 1, 3 }));

        result = new[] { 1, 2, 4 }.RangeExcept(0, 5).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0, 3 }));

        result = new[] { 3 }.RangeExcept(0, 1).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0 }));

        result = new[] { 1 }.RangeExcept(0, 1).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0 }));

        result = new[] { 1, 2, 4, 5, 6 }.RangeExcept(0, 5).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 0, 3 }));

        result = new[] { 0, 2, 4, 5, 6 }.RangeExcept(0, 5).ToArray();
        Contract.Requires(result.SequenceEqual(new[] { 1, 3 }));
    }
    [TestMethod]
    public void TestBinarySearch()
    {
        var testCase = new int[] { 0, 4, 10, 11 };

        var result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 0, Comparer<int>.Default.Compare);
        Contract.Assert(result == 0);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 1, Comparer<int>.Default.Compare);
        Contract.Assert(result == 1);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 3, Comparer<int>.Default.Compare);
        Contract.Assert(result == 1);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 4, Comparer<int>.Default.Compare);
        Contract.Assert(result == 1);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 5, Comparer<int>.Default.Compare);
        Contract.Assert(result == 2);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 9, Comparer<int>.Default.Compare);
        Contract.Assert(result == 2);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 10, Comparer<int>.Default.Compare);
        Contract.Assert(result == 2);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 11, Comparer<int>.Default.Compare);
        Contract.Assert(result == 3);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 12, Comparer<int>.Default.Compare);
        Contract.Assert(result == 4);

        result = BinarySearch.IndexPositionOf(i => testCase[i], testCase.Length, 16, Comparer<int>.Default.Compare);
        Contract.Assert(result == 4);



        var newTestCase = new int[] { 0, 2, 4 };



        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 0, Comparer<int>.Default.Compare);
        Contract.Assert(result == 0);
        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 1, Comparer<int>.Default.Compare);
        Contract.Assert(result == 1);
        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 2, Comparer<int>.Default.Compare);
        Contract.Assert(result == 1);
        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 3, Comparer<int>.Default.Compare);
        Contract.Assert(result == 2);
        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 4, Comparer<int>.Default.Compare);
        Contract.Assert(result == 2);
        result = BinarySearch.IndexPositionOf(i => newTestCase[i], newTestCase.Length, 5, Comparer<int>.Default.Compare);
        Contract.Assert(result == 3);

    }
    [TestMethod]
    public void TestIntervalCombinations()
    {
        var f0 = new Filling("0", new[] { true });
        var f01 = new Filling("01", new[] { true, true });
        var f1 = new Filling("1", new[] { false, true });
        var f12 = new Filling("12", new[] { false, true, true });
        var f02 = new Filling("02", new[] { true, false, true });
        var f2 = new Filling("2", new[] { false, false, true });

        Func<IReadOnlyList<Filling>, int, IEnumerable<Filling[]>> functionToTest1 = (options, length) => Combinatorics.AllFillingIntervalCombinations(options, length);
        Func<IReadOnlyList<Filling>, int, IEnumerable<Filling[]>> functionToTest2 = (options, length) => Combinatorics.AllFillingIntervalCombinations(options, length, (a, b) => false).Select(cwg => (Filling[])cwg.Combination);//tests the method where expanding is always impossible, and hence should reprocude the results of the other testmethod

        foreach (var functionToTest in new[] { functionToTest1, functionToTest2 })
        {
            IEnumerable<Filling[]> result = Combinatorics.AllFillingIntervalCombinations(f0.ToSingletonReadOnlyList(), 1).ToList();
            IEnumerable<Filling[]> expectation = new Filling[][] { new Filling[] { f0 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            Func<Filling[], int, IEnumerable<Filling[]>> getResult = (options, length) =>
                                                                                   {
                                                                                       int maxLength = options.Select(f => f.Occupation.Count).Max();
                                                                                       var equalLengthOptions = options.TruncateOrLengthenWithFalses(maxLength);

                                                                                       var initialFilling = Enumerable.Repeat(false, length)
                                                                                                                      .Concat(Enumerable.Repeat(true, maxLength - length))
                                                                                                                      .ToReadOnlyList();

                                                                                       return Combinatorics.AllFillingIntervalCombinations(equalLengthOptions, maxLength, initialFilling);
                                                                                   };
            result = getResult(new[] { f0, f1 }, 1);
            expectation = new Filling[][] { new Filling[] { f0 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f1 }, 2);
            expectation = new Filling[][] { new Filling[] { f0, f1 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f1, f12 }, 1);
            expectation = new Filling[][] { new Filling[] { f0 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f1, f12 }, 2);
            expectation = new Filling[][] { new Filling[] { f0, f1 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f1, f12 }, 3);
            expectation = new Filling[][] { new Filling[] { f0, f12 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f01, f12 }, 3);
            expectation = new Filling[][] { new Filling[] { f0, f12 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f01, f02 }, 3);
            expectation = new Filling[][] { };
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f01, f02, f1, f12 }, 3);
            expectation = new Filling[][] { new Filling[] { f0, f12 }, new Filling[] { f1, f02 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f01, f02, f1, f12, f2 }, 3);
            expectation = new Filling[][] { new Filling[] { f0, f12 }, new Filling[] { f1, f02 }, new Filling[] { f0, f1, f2 }, new Filling[] { f01, f2 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f01, f02, f1, f12, f2 }, 2);
            expectation = new Filling[][] { new Filling[] { f0, f1 }, new Filling[] { f01 } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            var f0b = new Filling("0b", new[] { true });
            var f01b = new Filling("01b", new[] { true, true });
            var f1b = new Filling("1b", new[] { false, true });
            var f12b = new Filling("12b", new[] { false, true, true });
            var f02b = new Filling("02b", new[] { true, false, true });
            var f2b = new Filling("2b", new[] { false, false, true });
            var f123 = new Filling("123", new[] { true, true, true });

            result = getResult(new[] { f0, f0b }, 1);
            expectation = new Filling[][] { new Filling[] { f0 }, new Filling[] { f0b } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f0b, f1, f1b }, 2);
            expectation = new Filling[][] { new Filling[] { f0, f1 }, new Filling[] { f0b, f1 }, new Filling[] { f0, f1b }, new Filling[] { f0b, f1b } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f0b, f1, f1b, f01, f01b }, 2);
            expectation = new Filling[][] { new Filling[] { f0, f1 }, new Filling[] { f0b, f1 }, new Filling[] { f0, f1b }, new Filling[] { f0b, f1b }, new Filling[] { f01 }, new Filling[] { f01b } }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));

            result = getResult(new[] { f0, f0b, f1, f01, f01b, f2, f12b, f12, f123 }, 3);
            expectation = new Filling[][]
                          {
                              new Filling[] { f0, f1, f2 },
                              new Filling[] { f0, f12 },
                              new Filling[] { f0, f12b },
                              new Filling[] { f0b, f1, f2 },
                              new Filling[] { f0b, f12 },
                              new Filling[] { f0b, f12b },
                              new Filling[] { f01, f2 },
                              new Filling[] { f01b, f2 },
                              new Filling[] { f123 }
                          }.ToList();
            Contract.Assert(result.ContainsSameElements(expectation, InterfaceWraps.ToEqualityComparer<Filling[]>(Equals, getHashCode)));
        }
    }
    [TestMethod]
    public void TestExpandableIntervalCombinations()
    {
        var f0 = new Filling("0", new[] { true });
        var f01 = new Filling("01", new[] { true, true });
        var f1 = new Filling("1", new[] { false, true });
        var f12 = new Filling("12", new[] { false, true, true });
        var f02 = new Filling("02", new[] { true, false, true });
        var f2 = new Filling("2", new[] { false, false, true });

        CanExpandFillingDelegate<Filling> canExpand = (filling, gap) => filling.Occupation.Count >= 2
                                                                     && filling.Occupation[1]
                                                                     && !filling.Occupation[0]
                                                                     && gap == new Gap(0, 1);

        IEnumerable<CombinationWithGaps<Filling>> result = Combinatorics.AllFillingIntervalCombinations(f0.ToSingletonReadOnlyList(), 1, canExpand).ToList();
        IEnumerable<Filling[]> expectation = new Filling[][] { new Filling[] { f0 } }.ToList();
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(f1.ToSingletonReadOnlyList(), 2, canExpand).ToList();
        expectation = new Filling[][] { new Filling[] { f1 } }.ToList();//even though f1 doesn't fill position 0
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(f1.ToSingletonReadOnlyList(), 3, canExpand).ToList();
        expectation = new Filling[0][].ToList();//f1 may expand to position 0, but not to position 2 (which wasn't available in the case above
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(f12.ToSingletonReadOnlyList(), 3, canExpand).ToList();
        expectation = new Filling[][] { new Filling[] { f12 } }.ToList();//even though f12 doesn't fill position 0
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(new[] { f1, f2 }, 3, canExpand).ToList();
        expectation = new Filling[][] { new Filling[] { f1, f2 } }.ToList();//f1 fills position 0
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(new[] { f0, f2 }, 3, canExpand).ToList();
        expectation = new Filling[0][].ToList();//position 1 is unfilled
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(new Filling[0], 1, canExpand).ToList();
        expectation = new Filling[0][].ToList();
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(new Filling[0], 3, canExpand).ToList();
        expectation = new Filling[0][].ToList();
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        result = Combinatorics.AllFillingIntervalCombinations(new[] { f1, f12 }, 3, canExpand).ToList();
        expectation = new Filling[][] { new Filling[] { f12 } }.ToList();//f12 fills position 0, f1 remains unused
        Contract.Assert(result.ContainsSameElements(expectation, Equals));

        //realization: an expansion shouldn't be applied when it could be omitted. Or at least, those without the expansion should be yielded first. 
        //So the output of AllFillingIntervalCombinations should be ordered by the number of expansions used
    }
    [TestMethod]
    public void TestFindMaxima()
    {
        var result = new int[] { 2, -1, 2, -1, 2 }.FindMaxima();
        var expectation = new int[] { 2, 2, 2 };

        Contract.Assert(result.SequenceEqual(expectation));
    }

    [TestMethod]
    public void TestSelectManySorted()
    {
        var result = new[] { 0, 2 }.SelectManySorted(i => new SortedEnumerable<int>(new[] { i, i + 1 })).ToList();

        Contract.Assert(result.SequenceEqual(new[] { 0, 1, 2, 3 }));
    }
    [TestMethod]
    public void TestSelectManySorted2()
    {
        var result = new[] { 2, 0 }.SelectManySorted(i => new SortedEnumerable<int>(new[] { i, i + 1 })).ToList();

        Contract.Assert(result.SequenceEqual(new[] { 0, 1, 2, 3 }));
    }
    [TestMethod]
    public void TestSelectManySorted3()
    {
        var init = new[] { 2, 0, 9, 50 };
        Func<int, IEnumerable<int>> selectMany = i => new[] { i, i + 3, i + 100 };

        var result = init.SelectManySorted(i => new SortedEnumerable<int>(selectMany(i))).ToList();
        var definitelyCorrectResult = init.SelectMany(selectMany).OrderBy(_ => _);

        Contract.Assert(result.SequenceEqual(definitelyCorrectResult));
    }

    private static bool Equals(Filling[]? a, Filling[]? b)
    {
        if (a is null)
            return b is null;
        if (b is null)
            return false;
        return a.Select(f => f.Tag).ContainsSameElements(b.Select(f => f.Tag));
    }
    private static bool Equals(CombinationWithGaps<Filling> a, Filling[] b)
    {
        return a.Combination.ContainsSameElements(b);
    }
    private static int getHashCode(Filling[] a)
    {
        if (a == null)
            return 0;
        return (int)a.Sum(element => (long)element.Tag!.GetHashCode()); // long since the Enumerable.Sum is in checked context
    }

    [TestMethod]
    public void TestToNullIfEmpty()
    {
        var sequence = new int[] { 1, 2, 3 };
        Contract.Assert(sequence.ToNullIfEmpty()!.SequenceEqual(sequence));

        sequence = new[] { 4 };
        Contract.Assert(sequence.ToNullIfEmpty()!.SequenceEqual(sequence));
    }

    [TestMethod, ExpectedException(typeof(AppSettingNotFoundException))]
    public void TestAppSettingNotFoundException()
    {
        throw new AppSettingNotFoundException();
    }

    [TestMethod]
    public void IntsAreValueTypes()
    {
        object v = new int() as ValueType;

        Contract.Assert(v != null);
    }
    [TestMethod]
    public void ValueTypeEquality()
    {
        Contract.Assert((int.Parse("20") as ValueType).Equals(int.Parse("20")));
    }
}
static class TestExtensions
{
    /// <summary> Returns the specified elements with false appeneded on each until they're all the same size. </summary>
    public static ReadOnlyCollection<Filling> FillWithFalses(this Filling[] fillings)
    {
        int desiredLength = fillings.Select(f => f.Occupation.Count).Max();
        return TruncateOrLengthenWithFalses(fillings, desiredLength);
    }
    /// <summary> Returns the specified elements with false appeneded on each until they're all the specified size. </summary>
    public static ReadOnlyCollection<Filling> TruncateOrLengthenWithFalses(this Filling[] fillings, int desiredLength)
    {
        return fillings.Select(f => new Filling(f.Tag!, f.Occupation.Take(desiredLength)
                                                                   .Concat(Enumerable.Repeat(false, Math.Max(0, desiredLength - f.Occupation.Count)))
                                                                   .ToReadOnlyList()))
                       .ToReadOnlyList();
    }
}

[TestClass]
public class SingleORDefaultTests
{
    [TestMethod]
    public void TestDefaultOnEmpty()
    {
        var result = EnumerableExtensions.SingletonOrDefault(new object[] { });
        Contract.Assert(result == null);
    }
    [TestMethod]
    public void TestNormalCase()
    {
        var input = new object();
        var result = EnumerableExtensions.SingletonOrDefault(new object[] { input });
        Contract.Assert(result == input);
    }
    [TestMethod]
    public void TestMultipleElementsCase()
    {
        var input = new object();
        var result = EnumerableExtensions.SingletonOrDefault(new object[] { input, input });
        Contract.Assert(result == null);
    }
    [TestMethod]
    public void OneMatchForPredicateTest()
    {
        var result = EnumerableExtensions.SingletonOrDefault(new[] { 1, 2 }, arg => arg == 1);
        Contract.Assert(result == 1);
    }
    [TestMethod]
    public void TwoMatchesForPredicateTest()
    {
        var result = EnumerableExtensions.SingletonOrDefault(new[] { 1, 1, 2 }, arg => arg == 1);
        Contract.Assert(result == 0);
    }
    [TestMethod]
    public void NoMatchForPredicateTest()
    {
        var result = EnumerableExtensions.SingletonOrDefault(new[] { 2 }, arg => arg == 1);
        Contract.Assert(result == 0);
    }
    [TestMethod]
    public void NoMatchForPredicateTest2()
    {
        var result = EnumerableExtensions.SingletonOrDefault(new[] { 2, 3 }, arg => arg == 1);
        Contract.Assert(result == 0);
    }
}
[TestClass]
public class ToLinkedDictionaryTests
{
    [TestMethod]
    public void Initialization()
    {
        var result = new ObservableCollection<object>().ToLiveDictionary<object, object, object?>(_ => { throw new UnreachableException(); }, _ => null);

        Contract.Assert(result.Count == 0);
    }
    [TestMethod]
    public void Addition()
    {
        var collection = new ObservableCollection<int>();
        var result = collection.ToLiveDictionary(i => i, i => i * i);

        collection.Add(5);
        Contract.Assert(result.Count == 1);
        Contract.Assert(result[5] == 25);
    }

    [TestMethod]
    public void InitialElements()
    {
        var collection = new ObservableCollection<int> { 5 };
        var result = collection.ToLiveDictionary(i => i, i => i * i);

        collection.Add(3);
        Contract.Assert(result.Count == 2);
        Contract.Assert(result[3] == 9);
        Contract.Assert(result[5] == 25);
    }
}
[TestClass]
public class SubsetTests
{
    [TestMethod]
    public void OverallTest()
    {
        var set = new Subset<int>(new[] { 0, 1, 2, 4 });
        var subset = new Subset<int>(set, new[] { 3 });

        Contract.Assert(subset.Count == 1);
        Contract.Assert(subset[0] == 4);
    }
}

[TestClass]
public class TakeWhileTests
{
    [TestMethod, ExpectedException(typeof(ArgumentNullException))]
    public void TestSequenceNotNull()
    {
        bool dummy;
        (null as IEnumerable<int>)!.TakeWhile(_ => true, out dummy);
    }

    [TestMethod, ExpectedException(typeof(ArgumentNullException))]
    public void TestPredicateNotNull()
    {
        bool dummy;
        new[] { 0 }.TakeWhile(null!, out dummy);
    }

    [TestMethod]
    public void TestEmptySequence()
    {
        bool dummy;
        var result = new int[] { }.TakeWhile(_ => { throw new InvalidOperationException(); }, out dummy);

        Contract.Assert(dummy);
        Contract.Assert(result.Count == 0);
    }

    [TestMethod]
    public void TestFirstElementMatches()
    {
        const int element = 0;
        bool dummy;
        var result = new[] { element }.TakeWhile(_ => true, out dummy);

        Contract.Assert(dummy);
        Contract.Assert(result.Count == 1);
        Contract.Assert(result[0] == element);
    }
    [TestMethod]
    public void TestSecondElementFails()
    {
        const int element0 = 0;
        const int element1 = 1;
        bool dummy;
        var result = new[] { element0, element1 }.TakeWhile(element => element < element1, out dummy);

        Contract.Assert(!dummy);
        Contract.Assert(result.Count == 1);
        Contract.Assert(result[0] == element0);
    }

}

[TestClass]
public class IsEmptyTests
{
    [TestMethod]
    public void EmptyIsEmptyTest()
    {
        var emptyCollection = Enumerable.Empty<int>();
        bool isEmpty = EnumerableExtensions.IsEmpty(ref emptyCollection);

        Assert.IsTrue(isEmpty);
    }
    [TestMethod]
    public void NonEmptyIsNonEmptyTest()
    {
        IEnumerable<int> sequence = new int[] { 1 };
        bool isEmpty = EnumerableExtensions.IsEmpty(ref sequence);

        Assert.IsFalse(isEmpty);
    }

    [TestMethod]
    public void SequenceEmptyInvarianceTest()
    {
        SequenceInvarianceTest(new int[0]);
    }
    [TestMethod]
    public void SequenceSingletonInvarianceTest()
    {
        SequenceInvarianceTest(new int[] { 1 });
    }
    [TestMethod]
    public void SequenceDoubletonInvarianceTest()
    {
        SequenceInvarianceTest(new int[] { 1, 2 });
    }
    [TestMethod]
    public void SequenceTripletonInvarianceTest()
    {
        SequenceInvarianceTest(new int[] { 1, 2, 3 });
    }
    void SequenceInvarianceTest(IEnumerable<int> originalSequence)
    {
        IEnumerable<int> sequence = originalSequence;
        EnumerableExtensions.IsEmpty(ref sequence);
        var retrievedSequence = sequence.ToList();

        Assert.IsTrue(retrievedSequence.SequenceEqual(originalSequence));
    }

    [TestMethod]
    public void EmptyIsEnumeratedOverOnlyOnce()
    {
        IsEnumeratedOverOnlyOnce(0);
    }
    [TestMethod]
    public void SingltonIsEnumeratedOverOnlyOnce()
    {
        IsEnumeratedOverOnlyOnce(1);
    }
    [TestMethod]
    public void DoubletonIsEnumeratedOverOnlyOnce()
    {
        IsEnumeratedOverOnlyOnce(2);
    }
    [TestMethod]
    public void TripletonIsEnumeratedOverOnlyOnce()
    {
        IsEnumeratedOverOnlyOnce(3);
    }
    void IsEnumeratedOverOnlyOnce(int sequenceLength)
    {
        int sideEffectExecutedCount = 0;
        Func<int, int> sideEffect = i => { sideEffectExecutedCount++; return i; };
        IEnumerable<int> sequence = Enumerable.Repeat(int.MaxValue, sequenceLength).Select(sideEffect);
        EnumerableExtensions.IsEmpty(ref sequence);

        //trigger side-effects:
        sequence.ToList();
        Assert.AreEqual(sequenceLength, sideEffectExecutedCount);
    }


    [TestMethod]
    public void EmptyCollectionIsDisposed()
    {
        CollectionIsDisposed(0, iterateOver: false);
    }
    [TestMethod]
    public void EmptyCollectionIteratedOverIsDisposed()
    {
        CollectionIsDisposed(0, iterateOver: true);
    }
    [TestMethod]
    public void SingletonCollectionIsDisposed()
    {
        CollectionIsDisposed(1, iterateOver: false);
    }
    [TestMethod]
    public void SingletonCollectionIteratedOverIsDisposed()
    {
        CollectionIsDisposed(1, iterateOver: true);
    }
    [TestMethod]
    public void DoubletonCollectionIsDisposed()
    {
        CollectionIsDisposed(2, iterateOver: false);
    }
    [TestMethod]
    public void DoubletonCollectionIteratedOverIsDisposed()
    {
        CollectionIsDisposed(2, iterateOver: true);
    }
    void CollectionIsDisposed(int sequenceLength, bool iterateOver)
    {
        var originalSequence = new EnumerableWithDisposableEnumerator(Enumerable.Repeat(int.MaxValue, sequenceLength));
        IEnumerable<int> sequence = originalSequence;
        EnumerableExtensions.IsEmpty(ref sequence);
        if (iterateOver) { sequence.ToList(); }

        Assert.IsTrue(originalSequence.IsDisposed == iterateOver);
    }

    [TestMethod]
    public void EmptyExceptionalEnumerableIsDisposedTest()
    {
        TestExceptionalEnumerableIsDisposed(0);
    }
    [TestMethod]
    public void SingletonExceptionalEnumerableIsDisposedTest()
    {
        TestExceptionalEnumerableIsDisposed(1);
    }
    [TestMethod]
    public void DoubletonExceptionalEnumerableIsDisposedTest()
    {
        TestExceptionalEnumerableIsDisposed(2);
    }
    public void TestExceptionalEnumerableIsDisposed(int indexThatThrows)
    {
        var originalSequence = new EnumerableWithDisposableEnumerator(EnumerableThatThrows());
        try
        {
            IEnumerable<int> sequence = originalSequence;
            EnumerableExtensions.IsEmpty(ref sequence);
            sequence.ToList();
        }
        catch { }
        Assert.IsTrue(originalSequence.IsDisposed);

        IEnumerable<int> EnumerableThatThrows()
        {
            for (int i = 0; i < indexThatThrows; i++)
            {
                yield return 0;
            }
            throw new Exception();
        }
    }

    [TestMethod]
    public void TestInclusiveRemainer()
    {
        IEnumerable<int> sequence = new int[] { 0, 1, 2 };
        var enumerator = sequence.GetEnumerator();
        enumerator.MoveNext();
        var result = enumerator.ToEnumerable(includeCurrent: true);
        var expected = sequence;

        Assert.IsTrue(expected.SequenceEqual(result));
    }
    [TestMethod]
    public void TestExclusiveRemainer()
    {
        IEnumerable<int> sequence = new int[] { 0, 1, 2 };
        var enumerator = sequence.GetEnumerator();
        enumerator.MoveNext();
        var result = enumerator.ToEnumerable(includeCurrent: false);
        var expected = sequence.Skip(1);

        Assert.IsTrue(expected.SequenceEqual(result));
    }

    [TestMethod]
    public void TestInclusiveEmptyRemainer()
    {
        IEnumerable<int> sequence = new int[] { 0 };
        var enumerator = sequence.GetEnumerator();
        enumerator.MoveNext();
        var result = enumerator.ToEnumerable(includeCurrent: true);
        var expected = sequence;

        Assert.IsTrue(expected.SequenceEqual(result));
    }

    [TestMethod]
    public void TestExclusiveEmptyRemainer()
    {
        IEnumerable<int> sequence = new int[] { 0 };
        IEnumerator<int> enumerator = new DisposableEnumerator(sequence.GetEnumerator());
        enumerator.MoveNext();
        var result = enumerator.ToEnumerable(includeCurrent: false);
        var expected = sequence.Skip(1);

        Assert.IsTrue(expected.SequenceEqual(result));
    }

    [TestMethod]
    public void TestEmptyRemainerMoveNextReturnedFalse()
    {
        IEnumerable<int> sequence = new int[] { 0 };
        IEnumerator<int> enumerator = new DisposableEnumerator(sequence.GetEnumerator());
        enumerator.MoveNext();
        enumerator.MoveNext();
        var result = enumerator.ToEnumerable(includeCurrent: null);
        var expected = sequence.Skip(1);

        Assert.IsTrue(expected.SequenceEqual(result));
    }

    class EnumerableWithDisposableEnumerator : IEnumerable<int>
    {
        private bool enumeratorHasBeenRetrieved;
        private readonly DisposableEnumerator enumerator;
        public bool IsDisposed => enumerator.IsDiposed;
        public EnumerableWithDisposableEnumerator(IEnumerable<int> sequence)
        {
            Contract.Requires(sequence != null);

            this.enumerator = new DisposableEnumerator(sequence.GetEnumerator());
        }
        public DisposableEnumerator GetEnumerator()
        {
            Contract.Requires(!enumeratorHasBeenRetrieved);

            enumeratorHasBeenRetrieved = true;
            return enumerator;
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    class DisposableEnumerator : IEnumerator<int>
    {
        private readonly IEnumerator<int> enumerator;
        public bool IsDiposed { get; private set; }

        public DisposableEnumerator(IEnumerator<int> enumerator)
        {
            Contract.Requires(enumerator != null);

            this.enumerator = enumerator;
        }

        public void Dispose()
        {
            this.enumerator.Dispose();
            this.IsDiposed = true;
        }


        public int Current => enumerator.Current;
        public bool MoveNext() => enumerator.MoveNext();
        public void Reset() => enumerator.Reset();
        object IEnumerator.Current => enumerator.Current;
    }
}
