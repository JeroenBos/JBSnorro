using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace JBSnorro
{
	public class SortedFacadeMapCollection<T, U> : FacadeMapCollection<T, U>, ISortedList<U>
	{
		public Func<U, U, int> Comparer { get; }

		public SortedFacadeMapCollection(IReadOnlyList<T> data, Func<T, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Select(selector).IsSorted(Comparer));
		}
		public SortedFacadeMapCollection(IReadOnlyList<T> data, Func<T, int, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Select(selector).IsSorted(Comparer));
		}
		public SortedFacadeMapCollection(IList<T> data, Func<T, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Select(selector).IsSorted(Comparer));
		}
		public SortedFacadeMapCollection(IList<T> data, Func<T, int, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Select(selector).IsSorted(Comparer));
		}
		public SortedFacadeMapCollection(IList data, Func<T, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Cast<T>().Select(selector).IsSorted(Comparer));
		}
		public SortedFacadeMapCollection(IList data, Func<T, int, U> selector, Func<U, U, int> comparer = null) : base(data, selector)
		{
			this.Comparer = comparer ?? Comparer<U>.Default.Compare;
			Contract.Requires(data.Cast<T>().Select(selector).IsSorted(Comparer));
		}
	}
	/// <summary> Represents a mapped wrapped read-only collection. Wrapped indicates that whenever the underlying collection changes, so does this one. </summary>
	public class FacadeMapCollection<T, U> : IReadOnlyList<U>
	{
		private readonly IList data;
		private readonly IReadOnlyList<T> data2;
		private readonly Func<T, int, U> selector;

		public U this[int index]
		{
			get
			{
				T item;
				if (data == null)
					item = data2[index];
				else
					item = (T)data[index];
				return selector(item, index);
			}
		}
		public int Count
		{
			get { return data?.Count ?? data2.Count; }
		}
		public FacadeMapCollection(IReadOnlyList<T> data, Func<T, U> selector) : this(data, (t, i) => selector(t)) { }
		public FacadeMapCollection(IReadOnlyList<T> data, Func<T, int, U> selector)
		{
			Contract.Requires(data != null);
			Contract.Requires(selector != null);

			this.data2 = data;
			this.selector = selector;
		}

		public FacadeMapCollection(IList<T> data, Func<T, U> selector) : this((IList)data, selector) { }
		public FacadeMapCollection(IList<T> data, Func<T, int, U> selector) : this((IList)data, selector) { }
		public FacadeMapCollection(IList data, Func<T, U> selector) : this(data, (t, i) => selector(t)) { }
		public FacadeMapCollection(IList data, Func<T, int, U> selector)
		{
			Contract.Requires(data != null);
			Contract.Requires(selector != null);

			this.data = data;
			this.selector = selector;
		}


		public IEnumerator<U> GetEnumerator()
		{
			if (this.data == null)
				return data2.Select(selector).GetEnumerator();
			return data.Cast<T>().Select(selector).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
