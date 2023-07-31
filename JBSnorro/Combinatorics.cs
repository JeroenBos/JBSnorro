using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Collections.Bits;
using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using JBSnorro.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JBSnorro.Global;

namespace JBSnorro
{
    /// <summary> Contains functionality in the area of combinatorics. </summary>
    public static class Combinatorics
    {
        /// <summary>
        /// Gets all permutations of the specified sequence. <seealso cref="https://stackoverflow.com/a/10630026/308451"/>
        /// </summary>
        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(this IReadOnlyCollection<T> sequence)
        {
            return sequence.GetPermutations(sequence.Count);
        }
        /// <summary>
        /// Gets all permutations of the specified sequence of the specified length. <seealso cref="https://stackoverflow.com/a/10630026/308451"/>
        /// </summary>
        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(this IEnumerable<T> sequence, int length)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (sequence.TryGetNonEnumeratedCount(out int count) && length > count) throw new ArgumentOutOfRangeException(nameof(length));


            if (length == 1)
            {
                return sequence.Select(t => new T[] { t });
            }

            return GetPermutations(sequence, length - 1).SelectMany(t => sequence.Where(e => !t.Contains(e)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        /// <summary> Generates all combinations for an array of a given length, where the possibilities at a particular index are specified by
        /// a delegate depending on the chosen possibility at the index before that. The possibilities of the first index are specified. </summary>
        /// <typeparam name="T"> The type of the possibilities. </typeparam>
        /// <param name="leafCount"> The number of possibilities to take in one combinations, i.e. the length of all yielded arrays. </param>
        /// <param name="firstLeafPossibilities"> The possibilities for the first position in the returned arrays. </param>
        /// <param name="getPossibilitiesForNext"> A function specifying the possibilities at the next index, given the selected possibilities at the indices before and at arg.Index.
        /// Must return null when arg.Index + 1 == leafCount. 
        /// If an empty enumerable is returned, that means that there are no possibilities are hence no array will be returned for that branch. 
        /// Invariant for the specified argument: 0 &lt;= argument.Index &lt; leafCount - 1. </param>
        /// <returns> Returns a series of arrays of length leafCount where each element has been specified.
        /// These arrays may be modified by the user, they are not reused by this method. </returns>
        public static IEnumerable<T[]> DependentCombinations<T>(int leafCount, IEnumerable<T> firstLeafPossibilities, Func<LeafChange<T>, IEnumerable<T>> getPossibilitiesForNext)
        {
            //this algorithm returns all combinations of leaves, where choosing one leaf affects the possibilities that can be chosen for the others.
            //so I need the list of possibilities for the first leaf and a function that returns the possibilities for the next leaf, given the previously selected leaves
            //this function can take the difference between what leaves changed at what index, or it can just take the entire of preceding chosen elements

            if (firstLeafPossibilities == null) throw new ArgumentNullException();
            if (getPossibilitiesForNext == null) throw new ArgumentNullException();

            //so basically this method traverses in a depth-first manner, each time it hits the last layer, a T[] is returned
            //each time is moves back one layer, it selects the next element in that layer and thereby gets the possibilities for the next layer
            //(which can then differ from what was previously possible in the next layer)

            var otherCombinations = new List<T[]>();//a DEBUG-only collection of all T[]'s already yielded
            return DependentCombinations(firstLeafPossibilities, getPossibilitiesForNext, 0, new T[leafCount])
                  .Select(result => (T[])result.Clone())
#if DEBUG
.Select(result =>
{
    Contract.Assume(!otherCombinations.Any(a => a.SequenceEqual(result)));
    otherCombinations.Add(result);
    return result;
})
#endif
;
        }
        /// <summary> See the other overload for comments. </summary>
        /// <param name="current"> The array which is being modified to set possibilities in. </param>
        /// <param name="leafIndex"> The index in the array 'current' which this method may set a possibility to. </param>
        private static IEnumerable<T[]> DependentCombinations<T>(this IEnumerable<T> possibilities, Func<LeafChange<T>, IEnumerable<T>> getPossibilitiesForNext, int leafIndex, T[] current)
        {
            //if there are no possibilities, simply no T[]'s are yielded
            if (leafIndex + 1 == current.Length)
            {
                foreach (T possibility in possibilities)
                {
                    var outOfRangePossibilities = getPossibilitiesForNext(new LeafChange<T>(leafIndex, possibility));
                    Contract.Assume(outOfRangePossibilities == null);

                    current[leafIndex] = possibility;
                    yield return current;
                }
            }
            else
            {
                foreach (T possibility in possibilities)
                {
                    current[leafIndex] = possibility;
                    var possibilitiesForNext = getPossibilitiesForNext(new LeafChange<T>(leafIndex, possibility));
                    var results = DependentCombinations(possibilitiesForNext, getPossibilitiesForNext, leafIndex + 1, current);
                    foreach (T[] result in results)
                    {
                        yield return result;
                    }
                }
            }
        }

        /// <summary> This class conveys the change of a chosen value for a leaf to the user, used in methods DependentPermutations&lt;T&gt;. </summary>
        public sealed class LeafChange<T>
        {
            /// <summary> The index of the leaf that is changed. </summary>
            public int Index { get; private set; }
            /// <summary> The element that was chosen to be placed at the index. </summary>
            public T ChosenElement { get; private set; }

            /// <summary> Creates a new leaf change. </summary>
            /// <param name="chosenElement"> The element that was chosen to be placed at the index. </param>
            /// <param name="index"> The index of the leaf that is changed. </param>
            [DebuggerHidden]
            public LeafChange(int index, T chosenElement)
            {
                this.Index = index;
                this.ChosenElement = chosenElement;
            }

            /// <summary> For debugging purposes. </summary>
            public override string ToString()
            {
                return string.Format("Chose {0} at index {1}", ChosenElement, Index);
            }
        }

        /// <summary> Returns all combinations of the specified elements of all sizes (starting small). </summary>
        /// <typeparam name="T"> The elements of the sequence. </typeparam>
        /// <param name="elements"> The elements to combine. Each element is assumed to be unique. </param>
        public static IEnumerable<T[]> AllCombinationsOfAllSizes<T>(this IReadOnlyList<T> elements)
        {
            for (int i = 1; i <= elements.Count; i++)
            {
                foreach (var combination in AllCombinations(elements, i))
                {
                    yield return combination;
                }
            }
        }
        /// <summary> Returns all combinations of the specified elements of a given size. </summary>
        /// <typeparam name="T"> The elements of the sequence. </typeparam>
        /// <param name="elements"> The elements to combine. Each element is assumed to be unique. </param>
        /// <param name="combinationSize"> The number of elements to choose per combination. </param>
        public static IEnumerable<T[]> AllCombinations<T>(this IReadOnlyList<T> elements, int combinationSize)
        {
            Contract.Requires(elements != null);
            Contract.Requires(combinationSize > 0);

            if (elements.Count < combinationSize)
                return EmptyCollection<T[]>.Enumerable;

            var ranges = EnumerableExtensions.AllCombinationsInRange(elements.Count, combinationSize);
            EnsureSingleEnumerationDEBUG(ref ranges);
            return ranges.Select(indices => indices.Map(i => elements[i]));
        }
        public static IEnumerable<T[]> AllCombinationsWithRepetitions<T>(this IReadOnlyList<T> elements, int combinationSize)
        {
            int[] indices = new int[combinationSize];

            while (true)
            {
                yield return indices.Map(i => elements[i]);

                int i;
                for (i = 0; i < indices.Length; i++)
                {
                    indices[i]++;
                    if (indices[i] != elements.Count)
                    {
                        break;
                    }
                    indices[i] = 0;
                }
                if (i == indices.Length)
                {
                    yield break;
                }

            }
        }
        /// <summary> Gets all combinations where you can specify per position which element can be present there. </summary>
        /// <param name="elementsPerPosition"> The possible elements per position. </param>
        public static IEnumerable<T[]> AllCombinations<T>(this IReadOnlyList<IReadOnlyList<T>> elementsPerPosition)
        {
            return AllCombinations(elementsPerPosition.Select(elems => elems.Count), (elementIndex, positionIndex) => elementsPerPosition[positionIndex][elementIndex]);
        }
        /// <summary> Gets all combinations where you can specify per position which element can be present there. </summary>
        /// <param name="elementsPerPosition"> The possible elements per position. </param>
        public static IEnumerable<T[]> AllCombinations<T>(this IList<IList<T>> elementsPerPosition)
        {
            return AllCombinations(elementsPerPosition.Select(elems => elems.Count), (elementIndex, positionIndex) => elementsPerPosition[positionIndex][elementIndex]);
        }
        private static IEnumerable<T[]> AllCombinations<T>(IEnumerable<int> counts, Func<int, int, T> select)
        {
            Contract.Requires(counts != null);
            Contract.LazilyRequires(ref counts, count => count >= 0);
            Contract.Requires(select != null);

            var result = EnumerableExtensions.Range(counts.ToList())
                                             .Select(elementIndices => elementIndices.Map(select));
            EnsureSingleEnumerationDEBUG(ref result);
            return result;
        }

        public interface IFillable
        {
            IReadOnlyList<bool> Occupation { get; }
        }

        public delegate bool CanExpandFillingDelegate<T>(T filling, Gap gap) where T : IFillable;
        /// <summary> Gets all combinations of options that fill the specified length, allowing some options to expand their occupation. 
        /// The resulting selected combinations are ordered by cumulative expansion length. </summary>
        /// <param name="options"> The option to fill the interval with. Each option holds the information of which spots it fills. </param>
        /// <param name="length"> The number of spots to fill. </param>
        /// <param name="initialFilling"> Specifies which indices are pre-filled. Specify null to indicate no pre-filling. </param>
        public static IEnumerable<CombinationWithGaps<T>> AllFillingIntervalCombinations<T>(this IReadOnlyList<T> options, int length, CanExpandFillingDelegate<T> canExpand, IReadOnlyList<bool> initialFilling = null) where T : class, IFillable
        {
            Contract.Requires(options != null);
            Contract.Requires(0 <= length);
            Contract.Requires(canExpand != null);

            int elementsYieldedCount_DEBUG = 0;
            IEnumerable<CombinationWithGaps<T>> result;
            result = Enumerable.Range(1, options.Count)
                               .SelectMany(i => AllCombinations(options, i))
                               .LazilyAssertForAll(_ => elementsYieldedCount_DEBUG++)
                               .Select(combination => CanCombinationFill(combination, combination.Select(element => element.Occupation), initialFilling, new Interval(0, length), canExpand))
                               .Where(NotNull)
                               .OrderBy(_ => _);//perhaps AllCombinations could be made such that this is guaranteed to be already in order? To improve performance

            EnsureSingleEnumerationDEBUG(ref result);
            return result;
        }



        /// <summary> Chooses one element from each sorted enumerable, and returns all such combinations in a specified order. </summary>
        /// <param name="sortedEnumerables"> The items per position. </param>
        public static ISortedEnumerable<T[]> AllOrderedCombinations<T>(this ReadOnlyCollection<ISortedEnumerable<T>> sortedEnumerables, Func<IReadOnlyList<T>, IReadOnlyList<T>, int> comparer)
        {
            Contract.Requires(sortedEnumerables != null);
            Contract.Requires(comparer != null);

            return new SortedEnumerable<T[]>(allOrderedCombinations(sortedEnumerables.Map(enumerable => new SortedReadOnlyList<T>(enumerable.ToList(), enumerable.Comparer)), comparer), comparer);
        }
        private static IEnumerable<T[]> allOrderedCombinations<T>(this ReadOnlyCollection<SortedReadOnlyList<T>> sortedEnumerables, Func<IReadOnlyList<T>, IReadOnlyList<T>, int> comparer)
        {
            Contract.Requires(sortedEnumerables != null);

            IEnumerable<T[]> result = sortedEnumerables.AllCombinations()
                                                       .OrderBy((a, b) => comparer(a, b));

            EnsureSingleEnumerationDEBUG(ref result);
            return result;

            //TODO: give specialized implementation. In that specialized algorithm, the T[]-T[] comparer should get an index of the only element that changed, 
            //so that it can be redirected directly to the individual element comparer. For that, the T[]'s have to be generated in single-change order. Only then may the following comparer work:

            //var individualComparer = sortedEnumerables[0].Comparer;
            //comparer = (a, b) =>
            //{
            //	for (int i = 0; i < a.Count; i++)
            //	{
            //		var elementComparisonResult = individualComparer(a[i], b[i]);
            //		if (elementComparisonResult != 0)
            //			return elementComparisonResult;
            //	}
            //	return 0;
            //};

            //var result = new T[sortedEnumerables.Count];
            //using (var enumerators = new DisposablesList<IEnumerator<T>>(sortedEnumerables.Select(sequence => sequence.GetEnumerator())))
            //{
            //	//fill initial array; or if impossible, return an empty enumerable
            //	int i = 0;
            //	foreach (var enumerator in enumerators)
            //	{
            //		if (enumerator.MoveNext())
            //		{
            //			result[i] = enumerator.Current;
            //		}
            //		else
            //		{
            //			yield break;
            //		}
            //	}
            //
            //  ...
            //}
        }
        /// <summary> Represents a combination that fills a specified length, possibly by having elements expanded to fill gaps. </summary>
        public sealed class CombinationWithGaps<T> : IComparable<CombinationWithGaps<T>> where T : IFillable
        {
            /// <summary> Gets the combination that is filled directly. </summary>
            public IReadOnlyList<T> Combination { get; }
            /// <summary> Gets the gaps that were filled by expanded adjacent options. May be empty. </summary>
            public List<Gap> Gaps { get; }
            /// <summary> Gets a cached value of the cumulative gap size of <code>this.Gaps</code>. </summary>
            private int cumulativeGapSize { get; }

            /// <summary> Creates a new combination with gaps. </summary>
            /// <param name="combination"> The combination that occupies contiguously all elements, except for the specified gaps. </param>
            /// <param name="gaps"> The gaps in this combination. May be empty. </param>
            public CombinationWithGaps(IReadOnlyList<T> combination, List<Gap> gaps)
            {
                Contract.Requires(combination != null);
                Contract.Requires(gaps != null);
                //checks that all filled positions are adjacent and without multiple occurrences
                //doesn't check whether it is sequential up to length, since it is not in scope here (unless I require all options' occupations to have equal size)
                IEnumerable<int> directlyFilledPositions = combination.SelectMany(option => option.Occupation.IndicesOf(_ => _));
                IEnumerable<int> indirectlyFilledPositions = gaps.SelectMany(gap => Enumerable.Range(gap.Start, gap.Length));
                Contract.Requires(directlyFilledPositions.Concat(indirectlyFilledPositions).OrderBy(_ => _).AreIncreasing());//sequential isn't required of those flagged 'alreadyBound'

                Combination = combination;
                Gaps = gaps;
                cumulativeGapSize = gaps.Sum(gap => gap.Length);
            }

            public CombinationWithGaps(IReadOnlyList<T> combination, int cumulativeGapSize)
            {
                this.Combination = combination;
                this.cumulativeGapSize = cumulativeGapSize;
            }

            /// <summary> Orders by smallest cumulative gap. </summary>
            int IComparable<CombinationWithGaps<T>>.CompareTo(CombinationWithGaps<T> other)
            {
                Contract.Requires(other != null);

                return this.cumulativeGapSize.CompareTo(other.cumulativeGapSize);
            }

            /// <summary> Creates one CombinationWithGaps out of the specified ones. </summary>
            public static CombinationWithGaps<T> Combine(IEnumerable<CombinationWithGaps<T>> combinations)
            {
                Contract.Requires(combinations != null);
                Contract.LazilyRequires(ref combinations, NotNull);
                Contract.Requires(combinations.SelectMany(combi => combi.AllIntervalsIn()).ToSortedList(IntervalExtensions.Compare).AreDisjoint());

                return new CombinationWithGaps<T>(combinations.SelectMany(combi => combi.Combination).ToReadOnlyList(),
                                                  combinations.SelectMany(combi => combi.Gaps).ToList());
            }

            private IEnumerable<Interval> AllIntervalsIn()
            {
                var occupiedIntervals = Combination.Select(selection => new Interval(selection.Occupation));
                var coveredGapIntervals = Gaps.Select(gap => new Interval(gap.Start, gap.End));

                return occupiedIntervals.LazilyAssertForAll(interval => !interval.IsEmpty)
                                        .Concat(coveredGapIntervals);
            }
        }

        /// <summary> Determines whether this combination of fillings can fill the entire length, where some fillings may be expanded. </summary>
        /// <returns> The specified combination with the gaps; or null if the entire length couldn't be filled (even with expansions). </returns>
        private static CombinationWithGaps<T> CanCombinationFill<T>(IReadOnlyList<T> combination, IEnumerable<IReadOnlyList<bool>> combinationFilling, IReadOnlyList<bool> initialFilling, Interval toFill, CanExpandFillingDelegate<T> canExpand) where T : class, IFillable
        {
            //an interval is filled if all positions up to length occur once in the combinationFilling and no positions after length occur.
            //if there are gaps, positions next to the gaps may be queried whether they are capable of filling the gaps. A gap may be multiple positions wide. 
            //a gap may not be filled by two partial expansions

            //these restricions are imposed for simplicity: perhaps later in general any gap may be filled by any position, but that doesn't seem required for now

            Contract.Requires(combination != null);
            Contract.RequiresForAll(combination, selection => toFill.Contains(new Interval(selection.Occupation)), "combination selected outside of interval to fill");

            Contract.Requires(!toFill.IsEmpty);
            Contract.Requires(toFill.StartInclusive);
            Contract.Requires(!toFill.EndInclusive);

            int length = combination[0].Occupation.Count;
            Func<int, T> getOptionThatFilledIndex = positionIndex =>
            {
                int i = combinationFilling.IndexOf(filling => positionIndex >= filling.Count ? false : filling[positionIndex]);
                if (i == -1)
                {
                    //in case the leaf at the specified position index was flagged 'alreadyBound'
                    Contract.Assert(initialFilling[positionIndex]);
                    return null;
                }
                return combination[i];
            };

            SortedList<int> filledPositionIndices = combinationFilling.ConcatIfNotNull(initialFilling)
                                                                      .SelectManySorted(elementFilling => elementFilling.IndicesOf(_ => _))
                                                                      .ToSortedList();//TODO: more lazily

            if (containsDoubles(filledPositionIndices))
            {
                return null;
            }

            List<Gap> gaps = findGaps(filledPositionIndices, toFill).ToList();
            foreach (Gap gap in gaps)
            {
                T substitute = null;
                //check whether the element to the left can fill the gap
                if (gap.Start != 0)
                {
                    var optionThatFilled = getOptionThatFilledIndex(gap.Start - 1);
                    if (optionThatFilled != null && canExpand(optionThatFilled, gap))
                        continue;
                }
                //check whether the element to the right can fill the gap
                if (gap.End < length)
                {
                    Contract.Assert(substitute == null, "gap is being filled from both sides");//just place 'else' before 'if' above: but realize that this then makes filling from the left preferable 
                    var optionThatFilled = getOptionThatFilledIndex(gap.End);
                    if (optionThatFilled != null && canExpand(optionThatFilled, gap))
                        continue;
                }

                //some gap could not be filled
                return null;
            }

            //all gaps were filled
            return new CombinationWithGaps<T>(combination, gaps);
        }
        private static bool containsDoubles(SortedList<int> filledPositionIndices)
        {
            return filledPositionIndices.Windowed2().Any(window => window.First == window.Second);
        }
        private static IEnumerable<Gap> findGaps(ISortedEnumerable<int> filledPositionIndices, Interval toFill)
        {
            int previousPresentIndex = toFill.Start - 1;
            foreach (int presentIndex in filledPositionIndices.ConcatIfNotLastElement(toFill.End))
            {
                if (presentIndex != previousPresentIndex + 1)
                {
                    yield return new Gap(previousPresentIndex + 1, presentIndex);
                }
                previousPresentIndex = presentIndex;
            }
        }


        /// <summary> Represents an interval between two filled positions (or bounds).  </summary>
        public struct Gap
        {
            /// <summary> The start of the interval. </summary>
            public int Start { get; }
            /// <summary> The (exclusive) end of the interval. </summary>
            public int End { get; }
            /// <summary> The number of positions in the interval. </summary>
            public int Length
            {
                get { return End - Start; }
            }

            /// <summary> Gets the occupation of this gap. </summary>
            /// <param name="length"> The length of the entire stretch that contains this gap. </param>
            public ImmutableBitArray GetOccupation(int length)
            {
                var result = new BitArray(length);
                for (int i = Start; i < End; i++)
                {
                    result[i] = true;
                }
                return new ImmutableBitArray(result);
            }

            /// <summary> Creates a new interval. </summary>
            public Gap(int start, int end) : this()
            {
                Contract.Requires(0 <= start);
                Contract.Requires(start <= end);

                Start = start;
                End = end;
            }

            public static bool operator ==(Gap a, Gap b)
            {
                return a.Start == b.Start && a.End == b.End;
            }
            public static bool operator !=(Gap a, Gap b)
            {
                return !(a == b);
            }
            public override bool Equals(object obj)
            {
                return obj is Gap && this == (Gap)obj;
            }
            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
            public override string ToString()
            {
                return $"Gap [{Start}, {End})";
            }

            public static implicit operator Geometry.Interval(Gap gap)
            {
                return new Geometry.Interval(gap.Start, gap.End);
            }
        }


        /// <summary> Gets all combinations of options that fill the specified length. </summary>
        /// <param name="options"> The option to fill the interval with. Each option holds the information of which spots it fills. </param>
        /// <param name="length"> The number of spots to fill. </param>
        /// <param name="initialFilling"> Specifies which indices are pre-filled. Specify null to indicate no pre-filling. </param>
        public static IEnumerable<T[]> AllFillingIntervalCombinations<T>(this IReadOnlyList<T> options, int length, IReadOnlyList<bool> initialFilling = null) where T : IFillable
        {
            return AllFillingIntervalCombinations(options, new Interval(0, length), initialFilling);
        }
        /// <summary> Gets all combinations of options that fill the specified length. </summary>
        /// <param name="options"> The option to fill the interval with. Each option holds the information of which spots it fills (from 0 to end.Length). </param>
        /// <param name="toFill"> The interval to fill. </param>
        /// <param name="initialFilling"> Specifies which indices are pre-filled. Specify null to indicate no pre-filling. </param>
        public static IEnumerable<T[]> AllFillingIntervalCombinations<T>(this IReadOnlyList<T> options, Interval toFill, IReadOnlyList<bool> initialFilling = null) where T : IFillable
        {
            //TODO: redo this method, it is terribly inefficient (and the other overload as well)
            //also immediately remove options that have overlap with the initial filling: or change design to ignore overlap with initial filling. Should that overlap then be full? 
            Contract.Requires(!toFill.IsEmpty);
            Contract.Requires(toFill.StartInclusive);
            Contract.Requires(!toFill.EndInclusive);
            Contract.Requires(toFill.Start >= 0);
            Contract.Requires(options != null);
            Contract.RequiresForAll(options, option => option != null);
            Contract.LazilyAssertForAll(options, option => option.Occupation.Count == toFill.End);

            if (initialFilling != null && initialFilling.All())
            {
                return EmptyCollection<T>.Array.ToSingleton();
            }

            var result = Enumerable.Range(1, options.Count)
                                   .SelectMany(i => AllCombinations(options, i))
                                   .Where(combination => DoesCombinationFill(combination.Select(element => element.Occupation).ConcatIfNotNull(initialFilling), toFill));

            EnsureSingleEnumerationDEBUG(ref result);
            return result;
        }
        /// <summary> Gets whether the specified combination of occupations fills the entire specified length. Is public only to serve debugging purposes. </summary>
        public static bool DoesCombinationFill(IEnumerable<IReadOnlyList<bool>> combination, Interval toFill)
        {
            Contract.Requires(!toFill.IsEmpty);
            Contract.Requires(toFill.StartInclusive);
            Contract.Requires(!toFill.EndInclusive);
            Contract.Requires(combination != null);
            Contract.RequiresForAll(combination, NotNull);
            EnsureSingleEnumerationDEBUG(ref combination);
            Contract.Requires(combination.Any(justAny => true));
            Contract.Requires(combination.Select(combi => combi.Count).AreEqual());

            //Contract.LazilyRequires(ref combination, occupation => occupation.Count == toFill.Length);//assumed the combinations are already shifted to start at the beginning of the specified interval

            //an interval is filled if all bits up to length occur once in the combinations and no bits after length occur.
            //that is the same as counting the bits and that they must equal length and that the indices of the bits are unique

            BitArray filled = new BitArray(toFill.Length);

            foreach (var occupation in combination)
            {
                Contract.Requires(occupation.Count >= toFill.Length);

                for (int i = 0; (ulong)i < filled.Length; i++)
                {
                    if (filled[i])
                    {
                        if (occupation[i + toFill.Start])
                        {
                            return false; // bit is doubly filled
                        }
                    }
                    else
                    {
                        filled[i] |= occupation[i + toFill.Start];
                    }
                }
            }

            return filled.IsFull();
        }
    }
}
