using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Algorithms
{
	public static class BinarySearch
	{
		/// <summary> Performs a binary search for an element in a sequence ignoring the element at a specified index.  </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="collection"> The collection in which to search. </param>
		/// <param name="ignoreIndex"> The index of the element in the collection to ignore. </param>
		/// <param name="element"> The element of the index to find. </param>
		/// <param name="comparer"> The comparer against which the collection is sorted (up to the ignored element). </param>
		/// <returns> the index the element would have been if it was part of the sequence (and the ignored element was not part of the sequence). </returns>
		public static int IndexOf<T>(this IList<T> collection, T element, int ignoreIndex, Func<T, T, int> comparer = null)
		{
			return IndexPositionOf(i => collection[i + (i >= ignoreIndex ? 1 : 0)], collection.Count - 1, element, comparer.OrDefault());
		}
		/// <summary> Returns the first occurence in specified list that matches the specified element according to the specified comparer. </summary>
		/// <typeparam name="T"> The type of the elements in the list. </typeparam>
		/// <typeparam name="U"> The type of the element to match elements in the list. </typeparam>
		/// <param name="sortedList"> The list to search in for a match. Must be transitive over the specified comparer with specified element. </param>
		/// <param name="element"> The element to search the match of. </param>
		/// <param name="comparer"> The comparer determining whether a </param>
		/// <returns> an occurence in the sorted list matching the specified element, or default(T) if no match was found. </returns>
		public static T FirstOrDefault<T, U>(this ISortedList<T> sortedList, U element, Func<T, U, int> comparer)
		{
			return FirstOrDefault<T, U>(i => sortedList[i], sortedList.Count, element, comparer);
		}
		/// <summary> Returns the first occurence in specified list that matches the specified element according to the specified comparer. </summary>
		/// <typeparam name="T"> The type of the elements in the list. </typeparam>
		/// <typeparam name="U"> The type of the element to match elements in the list. </typeparam>
		/// <param name="sortedList"> The list to search in for a match. Must be transitive over the specified comparer with specified element. </param>
		/// <param name="element"> The element to search the match of. </param>
		/// <param name="comparer"> The comparer determining whether a </param>
		/// <returns> an occurence in the sorted list matching the specified element, or default(T) if no match was found. </returns>
		public static T FirstOrDefault<T, U>(this SortedReadOnlyList<T> sortedList, U element, Func<T, U, int> comparer)
		{
			//TODO: First suggests the first is yielded, but actually any is yielded...
			Contract.Requires(sortedList != null);
			Contract.Requires(comparer != null);
			//transitivity is checked in FirstOrDefault

			return FirstOrDefault(i => sortedList[i], sortedList.Count, element, comparer);
		}

		public static int IndexOf<T>(this ISortedList<T> list, T element)
		{
			return IndexOf<T>(i => list[i], list.Count, element, list.Comparer);
		}
		/// <summary> Gets the index of the specified element in the specified list. </summary>
		public static int IndexOf<T>(IList<T> sortedList, T element, Func<T, T, int> comparer = null)
		{
			//NOTE: this method is not an extension method since it competes with EnumerableExtensions.IndexOf(IList<T>, T), which we generally prefer. This method assumes the list to be sorted

			return IndexOf<T>(i => sortedList[i], sortedList.Count, element, comparer);
		}

		public static int IndexOf<T>(Func<int, T> map, int count, T element, Func<T, T, int> comparer = null)
		{
			return IndexOf<T, T>(map, count, element, comparer.OrDefault());
		}
		/// <summary> Searches for an element that matches the specified element according to the specified comparer. </summary>
		/// <param name="result"> The element that matches, or default(<typeparam name="T"/>) if there was no match. </param>
		/// <param name="map"> The function serving as indexable enumerable in which a match is searched. 
		/// Its domain is [0, <paramref name="count"/>) and its codomain must be transitive according to the specified comparer. </param>
		/// <param name="count"> The cardinality of the domain of the map. </param>
		/// <param name="element"> The element of which a match is to be found. </param>
		/// <param name="comparer"> The comparer determining how the specified element compares to elements mapped to. </param>
		/// <returns> whether a matching element was found. </returns>
		public static bool TryFind<T, U>(Func<int, T> map, int count, U element, Func<T, U, int> comparer, out T result)
		{
			Contract.Requires(map != null);
			Contract.Requires(count >= 0);
			Contract.Requires(comparer != null);


			int possibleIndex = IndexOf(map, count, element, comparer);
			if (possibleIndex == -1)
			{
				result = default(T);
				return false;
			}
			else
			{
				result = map(possibleIndex);
				return true;
			}
		}
		public static T FirstOrDefault<T, U>(Func<int, T> map, int count, U element, Func<T, U, int> comparer)
		{
			Contract.Requires(map != null);
			Contract.Requires(comparer != null);
			Contract.Requires(comparer.IsTransitiveOver(map, count, element));

			T result;
			if (TryFind(map, count, element, comparer, out result))
				return result;
			else
				return default(T);
		}
		public static T First<T, U>(Func<int, T> map, int count, U element, Func<T, U, int> comparer)
		{
			T result;
			if (TryFind(map, count, element, comparer, out result))
				return result;
			else
				throw new Exception();
		}

		/// <summary> Gets the index in the specified map of the element, according to the specified comparer, or return -1 if no match was found. </summary>
		public static int IndexOf<T, U>(Func<int, T> map, int count, U element, Func<T, U, int> comparer)
		{
			Contract.Requires(map != null);
			Contract.Requires(comparer != null);
			Contract.Requires(count >= 0);

			if (count == 0)
				return -1;

			int possibleIndex = IndexPositionOf(map, count, element, comparer);
			Contract.Assert(0 <= possibleIndex);
			Contract.Assert(possibleIndex <= count);

			if (possibleIndex == count)
				return -1;

			T possibleMatch = map(possibleIndex);
			if (comparer(possibleMatch, element) == 0)
				return possibleIndex;
			else
				return -1;
		}

		/// <summary> Gets the index the specified element would have been at if it was in the specified collection. </summary>
		public static int IndexPositionOf<T>(this IList<T> list, T item, Func<T, T, int> comparer)
		{
			Contract.Requires(list != null);

			return IndexPositionOf<T, T>(i => list[i], list.Count, item, comparer);
		}
		/// <summary> Gets the index the specified element would have been at if it was in the specified collection. </summary>
		public static int IndexPositionOf<T, U>(Func<int, T> map, int count, U element, Func<T, U, int> comparer)
		{
			Contract.Requires(map != null);
			Contract.Requires(comparer != null);
			Contract.Requires(count >= 0);
			Contract.Requires(map.HasDomainUpTo(count));
			Contract.Requires(comparer.IsTransitiveOver(map, count, element));


			if (count == 0)
				return 0;

			int min = 0;
			int max = count;//max is exclusive

			while (min < max - 1)
			{
				int i = (max + min) / 2;
				Contract.Assert(i != max, "max is exclusive");

				T current = map(i);
				int comparisonResult = comparer(current, element);
				if (comparisonResult < 0)
				{
					//comparisonResult < 0, the first argument is smaller than second argument, 
					//hence the element sought for is larger than this[i], hence i and below can be ignored
					min = i + 1;
				}
				else if (comparisonResult > 0)
				{
					max = i;
				}
				else
				{
					//possibly the first of a sequence matching the element ought to be returned
					return i;
				}
			}
			if (min == count)
				return min;
			//return min;

			//one extra check is necessary since the range* is now [min, min - 1)
			//*range for finding equality. For finding a position, [min, min - 1] is the range
			{
				T potentialMatch = map(min);
				int comparisonResult = comparer(potentialMatch, element);
				if (comparisonResult < 0)
				{
					//element is larger than map(min)
					return min + 1;
				}
				else if (comparisonResult > 0)
				{
					//Contract.Assert(min == 0);
					return min;
				}
				else
				{
					//comparisonResult == 0
					return min;
				}
			}
		}

		/// <summary> Returns whether the domain of the specified map is up to (at least) the specified count. </summary>
		/// <typeparam name="T"> The type of the elements in the codomain of the map. </typeparam>
		/// <param name="map"> The map whose domain is to be checked. </param>
		/// <param name="count"> The number elements that are checked to be valid in the specified map.</param>
		private static bool HasDomainUpTo<T>(this Func<int, T> map, int count)
		{
			try
			{
				for (int i = 0; i < count; i++)
				{
					map(i);
				}
			}
			catch
			{
				return false;
			}
			return true;
		}
		/// <summary> Returns whether the specified comparer compares the canonical elements transitively over the codomain of the specified map. </summary>
		/// <param name="comparer"> The comparer to be checked for transitivity. </param>
		/// <param name="map"> The map of which the codomain should respect transitivity with respect to the specified comparer and canonical element. </param>
		/// <param name="count"> The cardinality of the domain of the specified map. </param>
		/// <param name="canonicalElement"> The element to check transitivity with. </param>
		private static bool IsTransitiveOver<T, U>(this Func<T, U, int> comparer, Func<int, T> map, int count, U canonicalElement)
		{
			bool previousIsLargerOrEqual = false;//to canonical element
			bool previousIsLarger = false;//than canonical element
			for (int i = 0; i < count; i++)
			{
				T imagedElement = map(i);
				int comparisonResult = comparer(imagedElement, canonicalElement);
				if (comparisonResult < 0)
				{
					//the element is smaller than the canonical element
					if (previousIsLargerOrEqual)
						return false;
				}
				else if (comparisonResult > 0)
				{
					previousIsLargerOrEqual = true;
					previousIsLarger = true;
				}
				else
				{
					//comparisonResult == 0
					if (previousIsLarger)
						return false;
					previousIsLargerOrEqual = true;
				}
			}
			return true;
		}
	}
}
