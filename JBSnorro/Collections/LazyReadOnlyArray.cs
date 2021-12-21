using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> Represents a collection with fixed size whose elements are computed on demand (without caching them). </summary>
	[DebuggerDisplay("Count: {Count}")]
	public class LazyReadOnlyArray<T> : IReadOnlyList<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Func<int, T> selector;
		/// <summary> Gets the number of elements in this collection.  </summary>
		public int Count { get; }

		/// <summary> Creates a new <see cref="LazyReadOnlyArray{T}"/>. </summary>
		/// <param name="selector"> A function determining the element in this collection at the argument index. </param>
		/// <param name="count"> The number of elements this collection has. </param>
		[DebuggerHidden]
		public LazyReadOnlyArray(Func<int, T> selector, int count)
		{
			Contract.Requires(selector != null);
			Contract.Requires(0 <= count);

			this.selector = selector;
			this.Count = count;
		}

		/// <summary> Gets the element in this collection at the specified index. </summary>
		/// <param name="index"> The index of the element to fetch. </param>
		public T this[int index]
		{
			get
			{
				Contract.Requires(0 <= index);
				Contract.Requires(index < this.Count);
				return selector(index);
			}
		}
		/// <summary> Gets an enumerator that iterates through this collection. </summary>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
