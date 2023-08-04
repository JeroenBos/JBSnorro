using JBSnorro.Algorithms;
using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	public static class SortedEnumerableExtensions
	{
		/// <summary> Yields all elements that are between the elements in the specified sorted sequence. So basically, this is exclusion of the sequence from the range of the sequence. </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="sequence"> The sequence of the elements that are not yielded. </param>
		/// <param name="increment"> A function specifying the next element given one. </param>
		/// <param name="start"> The start of the range of all elements to yield. </param>
		/// <param name="end"> The end of the range of all elements to yield. </param>
		/// <param name="equalityComparer"> The equality comparer used for determining whether an element in the specified sequence matches that in the range. </param>
		public static IEnumerable<T> RangeExcept<T>(this ISortedEnumerable<T> sequence, Func<T, T> increment, T start, T end, IEqualityComparer<T>? equalityComparer = null)
		{
			Contract.Requires(sequence != null);
			Contract.Requires(increment != null);
			Contract.Requires(sequence.IsSorted(sequence.Comparer));
			equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

			T previouslyExcludedElement = start;
			bool first = true;
			foreach (var excludedElement in sequence.Concat(end))
			{
				foreach (var elementBelowExcludedElement in Range(previouslyExcludedElement, increment, excludedElement, equalityComparer, first))
				{
					if (equalityComparer.Equals(end, elementBelowExcludedElement))
						yield break;
					yield return elementBelowExcludedElement;
				}
				if (equalityComparer.Equals(end, excludedElement))
					yield break;
				previouslyExcludedElement = excludedElement;
				first = false;
			}
		}
		/// <summary> Yields all elements that are between the elements in the specified sorted sequence. So basically, this is exclusion of the sequence from the range of the sequence. </summary>
		/// <param name="sequence"> The sequence of the elements that are not yielded. </param>
		/// <param name="start"> The (inclusive) start of the range of all elements to yield. </param>
		/// <param name="end"> The (exclusive) end of the range of all elements to yield. </param>
		[DebuggerHidden]
		public static IEnumerable<int> RangeExcept(this ISortedEnumerable<int> sequence, int start, int end)
		{
			return RangeExcept(sequence, i => i + 1, start, end);
		}

		private static IEnumerable<T> Range<T>(T start, Func<T, T> increment, T end, IEqualityComparer<T> equalityComparer, bool startInclusive)
		{
			Contract.Requires(increment != null);
			Contract.Requires(equalityComparer != null);
			for (T current = start; !equalityComparer.Equals(current, end); current = increment(current))
			{
				if (startInclusive)
					yield return current;
				else
					startInclusive = true;
			}
		}

		public static SortedList<TOut> Map<TIn, TOut>(this SortedList<TIn> sequence, Func<TIn, TOut> map)
		{
			var result = new SortedList<TOut>();
			foreach (TIn element in sequence)
				result.Insert(result.Count, map(element));
			return result;
		}

		/// <summary> Maps the sorted sequence into another sorted sequence. </summary>
		[DebuggerHidden]
		public static ISortedEnumerable<TResult> SelectSorted<TSource, TResult>(this ISortedEnumerable<TSource> sequence, Func<TSource, TResult> selector, Func<TResult, TResult, int>? resultComparer = null)
		{
			Contract.Requires<ArgumentNullException>(sequence != null);
			Contract.Requires<ArgumentNullException>(selector != null);

			return new SortedEnumerable<TResult>(((IEnumerable<TSource>)sequence).Select(selector), resultComparer);
		}

		/// <summary> Maps the sorted sequence into another sorted sequence. </summary>
		[DebuggerHidden]
		public static ISortedEnumerable<TResult> SelectSorted<TSource, TResult>(this ISortedEnumerable<TSource> sequence, Func<TSource, TResult> selector, Func<Func<TSource, TSource, int>, Func<TResult, TResult, int>> comparerSelector)
		{
			Contract.Requires<ArgumentNullException>(sequence != null);
			Contract.Requires<ArgumentNullException>(selector != null);

			return new SortedEnumerable<TResult>(((IEnumerable<TSource>)sequence).Select(selector), comparerSelector(sequence.Comparer));
		}
		/// <summary>
		/// Filters the specified sorted sequence on the specified predicate.
		/// </summary>
		public static ISortedEnumerable<TSource> WhereSorted<TSource>(this ISortedEnumerable<TSource> sequence, Func<TSource, bool> predicate)
		{
			Contract.Requires(sequence != null);
			Contract.Requires(predicate != null);

			return new SortedEnumerable<TSource>(sequence.Where(predicate), sequence.Comparer);
		}
		/// <summary>
		/// Inserts the specified item in the specified collection in order, assuming the specified collection is already in the order determined by the specified comparer.
		/// </summary>
		/// <param name="collection"> The collection to insert the item in. </param>
		/// <param name="item"> The item to insert in the collection in order. </param>
		/// <param name="comparer"> The comparer determining the order of the collection; specified null to use the default comparer. </param>
		public static void Insert<T>(this IList<T> collection, T item, IComparer<T>? comparer = null)
		{
			Contract.Requires(collection != null);

			comparer = comparer ?? Comparer<T>.Default;

			Contract.Requires(collection.IsSorted(comparer.Compare), "The specified collection is not sorted against the specified comparer");

			int indexToPlaceItem = BinarySearch.IndexPositionOf(collection, item, comparer.Compare);

			collection.Insert(indexToPlaceItem, item);
		}
	}
}
