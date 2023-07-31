using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> Represents a subset selected out of another collection by specifying the indices of which elements to select. </summary>
	public class Subset<T> : IReadOnlyList<T>
	{
		/// <summary> An immutable set of which this set is a (not necessarily proper) subset. </summary>
		private readonly IReadOnlyList<T> items;
		/// <summary> The immutable indices in 'items' that are part of this subset. Can be null, in which case all items are considered to be in this set. </summary>
		private readonly IList<int> indices;

		/// <summary> Gets the element in this subset at the specified index. </summary>
		public T this[int index]
		{
			get
			{
				Contract.Requires(0 <= index && index < this.Count);

				if (indices == null)
				{
					return items[index];
				}
				else
				{
					return items[indices[index]];
				}
			}
		}
		/// <summary> Gets the number of elements in this subset. </summary>
		public int Count
		{
			get { return indices?.Count ?? items.Count; }
		}

		/// <summary> Creates a new set (which isn't a subset) from the specified elements. </summary>
		public Subset(IEnumerable<T> elements)
		{
			Contract.Requires(elements != null);

			this.items = elements.ToReadOnlyList();
			this.indices = null;
		}
		/// <summary> Creates a subset from the specified collection by taking the elements at the specified indices. </summary>
		public Subset(Subset<T> wholeCollection, IEnumerable<int> indices)
		{
			Contract.Requires(wholeCollection != null);
			Contract.Requires(indices != null);

			this.items = wholeCollection.items;
			if (wholeCollection.indices == null)
			{
				this.indices = indices.ToList();
			}
			else
			{
				this.indices = indices.Select(i => wholeCollection.indices[i]).ToList();
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public override int GetHashCode()
		{
			// throws so that I know when I accidentally forgot to pass along an equality comparer
			throw new InvalidOperationException();
		}
		public override bool Equals(object? obj)
		{
			// throws so that I know when I accidentally forgot to pass along an equality comparer
			throw new InvalidOperationException();
		}
	}

	public static class SubsetExtensions
	{
		public static Subset<T> Subset<T>(this IReadOnlyList<T> collection, IEnumerable<int> indices)
		{
			Contract.Requires(collection != null);
			if (collection is Subset<T> s)
			{
				return new Subset<T>(s, indices);
			}

			// expensive:
			return new Subset<T>(indices.Select(i => collection[i]));
		}
	}
}
