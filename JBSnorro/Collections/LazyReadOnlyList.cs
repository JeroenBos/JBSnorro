using JBSnorro.Diagnostics;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> A readonly wrapper around an enumerable, caching its elements as it is lazily iterated over. </summary>
	/// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
	public sealed class LazyReadOnlyList<T> : ILazilyCachable<T>, IReadOnlyList<T>, IDisposable
	{
		/// <summary> Backingfield. </summary>
		[NotNull]
		private readonly LazyList<T> data;

		/// <summary> Creates a new lazy readonly enumerable from the specified enumerable. </summary>
		/// <param name="enumerable"> The enumerable to wrap around. </param>
		public LazyReadOnlyList([NotNull] IEnumerable<T> enumerable)
		{
			this.data = new LazyList<T>(enumerable);
		}

		/// <summary> Creates a lazy list that caches the results of the specified function. </summary>
		/// <param name="generator"> The function generating the elements to cache. The specified argument is the index. </param>
		public LazyReadOnlyList([NotNull] Func<int, T> generator)
		{
			this.data = new LazyList<T>(generator);
		}

		/// <summary> Gets the elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
		public IEnumerable<T> Query
		{
			get { return data.Query; }
		}
		/// <summary> Gets all cached elements. </summary>
		public IEnumerable<T> Cache
		{
			get { return data.Cache; }
		}
		/// <summary> Gets all elements of the original enumerable. First yields the cached elements 
		/// and afterwards elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
		public IEnumerable<T> Original
		{
			get { return data.Original; }
		}
		/// <summary> Gets whether all elements of the original sequence are cached. </summary>
		public bool FullyCached
		{
			get { return data.FullyCached; }
		}
		/// <summary> Gets the number of cached elements. </summary>
		public int CachedCount
		{
			get { return data.CachedCount; }
		}
		/// <summary> Gets the element at the specific index, quering for more elements from the original enumerable if necessary. </summary>
		/// <param name="index"> The index of the element in the original enumerable to fetch. </param>
		public T this[int index]
		{
			get { return this.data[index]; }
		}
		public bool TryGetAt(int index, out T value)
		{
			return data.TryGetAt(index, out value);
		}

		/// <summary> Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
		public void Dispose()
		{
			this.data.Dispose();
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
		/// <summary> Gets the number of elements in the sequence. </summary>
		public int Count
		{
			get { return this.data.Count; }
		}
	}
}
