using JBSnorro.Diagnostics;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> This type represents a list that caches a lazily queried enumerable. After the specified enumerable has been exhausted, this collection functions exactly as a <see cref="List{T}"/>. </summary>
	/// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
	public sealed class LazyList<T> : IList<T>, ILazilyCachable<T>, IDisposable
	{
		/// <summary> Gets the default capacity for a new list in case it is not explicitly specified. </summary>
		private const int DEFAULT_CAPACITY = 4;
		/// <summary> The value of the field <code>sequence</code> after it has been exhausted. </summary>
		private const IEnumerator<T> exhaustedEnumerator = null;

		/// <summary> The cache of all yielded nodes from the enumerator. Is never null. </summary>
		private readonly List<T> cache;
		/// <summary> The enumerator of the encapsulated enumerable. Is iterated over only once and is set to <code>null</code> if it has been exhausted. </summary>
		private IEnumerator<T> sequence;

		/// <summary> Gets the elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
		public IEnumerable<T> Query
		{
			get
			{
				if (sequence != exhaustedEnumerator)
				{
					while (sequence.MoveNext())
					{
						cache.Add(sequence.Current);
						yield return sequence.Current;
					}
					this.Dispose();
				}
			}
		}
		/// <summary> Gets all cached elements. </summary>
		public IEnumerable<T> Cache
		{
			get { return cache.Select(x => x); }
		}
		/// <summary> Gets all elements of the original enumerable. First yields the cached elements 
		/// and afterwards elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
		public IEnumerable<T> Original
		{
			get { return this.Cache.Concat(this.Query); }
		}
		/// <summary> Gets whether all elements in the original enumerable have been enumerated over. This differs subtly from <see cref="FullyCached"/>. </summary>
		public bool CheckFullyCached()
		{
			return !Query.Any();
		}
		/// <summary> Gets whether the end of the original enumerable has been reached. </summary>
		public bool FullyCached
		{
			get
			{
				return this.sequence == exhaustedEnumerator;
				//but in the case where the list _is_ fully cached but doesn't know it yet because it hasn't been queried for the next element (that doesn't exist)
				//say this returns false, then you expect this.TryGetAt(CachedCount, out t) to return true, but it may not....

				//to prevent confusion about this, changed the name from FullyCached to OriginalExhausted
			}
		}
		/// <summary> Gets the number of cached elements. </summary>
		public int CachedCount
		{
			get { return this.cache.Count; }
		}
		/// <summary> Gets whether the specified count is equal to the total number of elements in this lazy list, caching the elements at most until the specified count. </summary>
		/// <param name="proposedCount"> The count to compare the number of elements in this lazy list to. </param>
		public bool CountEquals(int proposedCount)
		{
			CacheUpTo(proposedCount);//don't add one to the argument proposedCount, because the parameter requests an index, not a count 

			Contract.Assume(FullyCached || CachedCount > proposedCount);//no >= operator here, proposedCount is used as index in CacheUpTo 

			return FullyCached && CachedCount == proposedCount;
		}
		/// <summary> Gets whether this lazy list contains at least the specified number of elements, caching the elements at most until the specified count. </summary>
		public bool CountsAtLeast(int minimimCount)
		{
			CacheUpTo(minimimCount);
			return CachedCount >= minimimCount;
		}

		/// <summary> Repaces the elements in the interval from <code>start</code> to <code>start + count</code> by the specified items. </summary>
		/// <param name="start"> The index to replace the items. </param>
		/// <param name="count"> The number of items to remove. </param>
		/// <param name="substitutions"> The items to be placed at <code>start</code>. </param>
		public void Substitute(int start, int count, IEnumerable<T> substitutions)
		{
			Contract.Requires(start >= 0);
			Contract.Requires(count >= 0);
			Contract.Requires(substitutions != null);

			CacheUpTo(start + count - 1);
			if (cache.Count < start + count) throw new ArgumentOutOfRangeException("count", "start + count is larger than sequence length");

			var cachedSubstitutions = substitutions.ToList();
			for (int i = 0; i < Math.Min(cachedSubstitutions.Count, count); i++)
				cache[start + i] = cachedSubstitutions[i];
			if (count > cachedSubstitutions.Count)
			{
				cache.RemoveRange(start + cachedSubstitutions.Count, count - cachedSubstitutions.Count);
			}
			else if (count < cachedSubstitutions.Count)
			{
				cache.InsertRange(start + cachedSubstitutions.Count, cachedSubstitutions.Skip(count));
			}
		}

		/// <summary> Creates a new lazy enumerable cache from the specified enumerable with a specified initial capacity for the cache list. </summary>
		/// <param name="enumerable"> The enumerable that this wrapper enumerates over and caches. </param>
		/// <param name="initialCapacity"> The initial capacity for the cache list. </param>

		public LazyList([NotNull] IEnumerable<T> enumerable, int initialCapacity = DEFAULT_CAPACITY)
		{
			Contract.Requires(enumerable != null);

			this.cache = new List<T>(initialCapacity);
			var x = enumerable.GetEnumerator();
			this.sequence = x;
		}

		/// <summary> Creates a lazy list that caches the results of the specified function. </summary>
		/// <param name="generator"> The function generating the elements to cache. The specified argument is the index. </param>
		/// <param name="initialCapacity"> The initial capacity of this list. </param>
		public LazyList([NotNull] Func<int, T> generator, int initialCapacity = DEFAULT_CAPACITY)
			: this(Enumerable.Range(0, int.MaxValue).Select(generator), initialCapacity)
		{
		}

		/// <summary> Caches the sequence up to and including the element at the specified index. 
		/// If the index is larger than or equal to the length of the sequence, the sequence is completely cached. </summary>
		/// <param name="index"></param>
		private void CacheUpTo(int index)
		{
			index -= cache.Count;
			if (!FullyCached && index >= 0)
			{
				// this just iterates over the Query (partially) to populate the cache
				foreach (var _ in Query.TakeWhile(_ => index-- > 0))
				{
				}
			}
		}
		/// <summary> Caches the sequence completely. </summary>
		private void CacheAll()
		{
			// this method just iterates over the entire Query to populate the cache
			foreach (var _ in Query)
			{
			}
		}

		/// <summary> Tries to get the element at the specified index, returning whether it succeeded. </summary>
		/// <param name="index"> The index of the element to get. </param>
		/// <param name="value"> The element, if found. </param>
		public bool TryGetAt(int index, out T value)
		{
			if (index >= 0)
			{
				CacheUpTo(index);
				if (index < cache.Count)
				{
					value = cache[index];
					return true;
				}
			}
			value = default(T);
			return false;
		}
		/// <summary> Gets the element at the specific index, quering for more elements from the original enumerable if necessary. </summary>
		/// <param name="index"> The index of the element in the original enumerable to fetch. </param>
		public T this[int index]
		{
			get
			{
				CacheUpTo(index);
				return this.cache[index];
			}
			set
			{
				CacheUpTo(index);
				this.cache[index] = value;
			}
		}
		/// <summary> Adds an item to the end of the <see cref="T:System.Collections.Generic.ICollection`1"/>. </summary>
		/// <param name="item"> The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public void Add(T item)
		{
			CacheAll();
			this.cache.Add(item);
		}
		/// <summary> Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>, including those not yet queried. </summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
		public void Clear()
		{
			CacheAll();
			this.cache.Clear();
		}
		/// <summary> Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value. </summary>
		/// <returns> true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. </returns>
		/// <param name="item"> The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(T item)
		{
			return this.Original.Contains(item);
		}
		/// <summary>  Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an 
		/// <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index. </summary>
		/// <param name="array"> The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			CacheAll();
			this.cache.CopyTo(array, arrayIndex);
		}
		/// <summary> Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>. </summary>
		/// <returns> true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. 
		/// This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>. </returns>
		/// <param name="item"> The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public bool Remove(T item)
		{
			int index = this.IndexOf(item);
			if (index < 0)
				return false;
			this.cache.RemoveAt(index);
			return true;
		}
		/// <summary> Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>. </summary>
		/// <returns> The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>. </returns>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // has side effects
		public int Count
		{
			get
			{
				this.CacheAll();
				return this.CachedCount;
			}
		}
		/// <summary> Gets false. </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}
		/// <summary> Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>. </summary>
		/// <returns> The index of <paramref name="item"/> if found in the list; otherwise, -1. </returns>
		/// <param name="item"> The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		public int IndexOf(T item)
		{
			return this.Original.IndexOf(item);
		}
		/// <summary> Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index. </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void Insert(int index, T item)
		{
			CacheUpTo(index - 1);
			this.cache.Insert(index, item);//throws if necessary
		}
		/// <summary> Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index. </summary>
		/// <param name="index">The zero-based index of the item to remove.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void RemoveAt(int index)
		{
			CacheUpTo(index);
			this.cache.RemoveAt(index);//throws if necessary
		}
		/// <summary> Disposes of the underlying enumerator of the original enumerable. </summary>
		public void Dispose()
		{
			if (this.sequence != exhaustedEnumerator)
			{
				this.sequence.Dispose();
				this.sequence = exhaustedEnumerator;
			}
		}
		/// <summary> Gets the enumerator that iterates over all cached and uncached elements. An uncached element is cached before it is yielded. </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return this.Original.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public override string ToString()
		{
			return string.Format("CachedCount: {0}, FullyCached: {1}", CachedCount, this.FullyCached);
		}
	}
}
