using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using System;
using System.Diagnostics;

namespace JBSnorro.Collections
{
    /// <summary>
    /// Represents a collection of lazily computed elements (and are cached upon first computation).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count: {Count}")]
	public class CachedReadOnlyCollection<T> : IReadOnlyList<T>
	{
		private readonly Func<int, T> selector;
		private readonly BitArray selected;
		private readonly T[] data;

		public T this[int index]
		{
			get
			{
				Contract.Requires(0 <= index && index < this.Count);

				if (!selected[index])
				{
					data[index] = selector(index);
					selected[index] = true;
				}
				return data[index];
			}
		}
		public int Count => data.Length;

		public CachedReadOnlyCollection(int count, Func<int, T> selector)
		{
			Contract.Requires(selector != null);
			Contract.Requires(0 <= count);

			this.selector = selector;
			this.selected = new BitArray(count);
			this.data = new T[count];
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Gets the index of the first element in this collection that has already been cached that matches the specified predicate.
		/// </summary>
		public int IndexOfCached(Func<T, bool> predicate)
		{
			Contract.Requires(predicate != null);

			for (int i = 0; i < this.Count; i++)
			{
				if (selected[i] && predicate(data[i]))
					return i;
			}

			return -1;
		}
	}
}
