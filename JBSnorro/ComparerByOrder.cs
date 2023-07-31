using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> This type can compare a finite number of objects by having all comparable objects stored in order. </summary>
	public sealed class ComparerByOrder<T> : IComparer<T>
	{
		/// <summary> Gets the reference to all elements, ordered from low to high. Elements that compare equally are stored in lists. </summary>
		private readonly List<List<T>> sortedElements;
		/// <summary> A backingfield for Precedences. </summary>
		private readonly List<ReadOnlyCollection<T>> readOnlyElements;
		/// <summary> Gets the elements sorted by comparison from low to high, and grouped for equal compare. </summary>
		public ReadOnlyCollection<ReadOnlyCollection<T>> Elements { get; private set; }

		/// <summary> Creates a new empty precedence comparer. </summary>
		public ComparerByOrder()
		{
			this.sortedElements = new List<List<T>>();
			this.readOnlyElements = new List<ReadOnlyCollection<T>>();
			this.Elements = new ReadOnlyCollection<ReadOnlyCollection<T>>(this.readOnlyElements);
		}

		/// <summary> Adds the element to this comparer stating that it compares to the other specified element. </summary>
		/// <param name="element"> The element to add to this comparer. </param>
		/// <param name="elementOfEqualPrecedence"> The element to which the other element compares. </param>
		public void Add(T element, T elementOfEqualPrecedence)
		{
			Contract.Requires(!this.Contains(element));
			Contract.Requires(this.Contains(elementOfEqualPrecedence));

			Add(GetComparisonIndex(elementOfEqualPrecedence), element);
		}
		/// <summary> Adds the element, stating that it compares to elements already present in <code>this.Precedences</code> at the specified index. </summary>
		/// <param name="element"> The element to add to this comparer. </param>
		/// <param name="i"> The index in <code>this.Precedences</code> of the elements that compare to the specified element. </param>
		public void Add(int i, T element)
		{
			Contract.Requires(0 <= i && i < this.Elements.Count);
			Contract.Requires(!Contains(element));

			sortedElements[i].Add(element);
		}
		/// <summary> Adds the specified element, stating that it compares to no element already present in <code>this.Precedences</code>.
		///  To this comparer, it is higher than <code>Precedences[i][0]</code> and lower than <code>Precedences[i + 1][0]</code>. </summary>
		/// <param name="element"> The element to add to this comparer. </param>
		/// <param name="i"> The index in the <code>this.Precedences</code> where the specified element will be inserted. </param>
		public void Insert(int i, T element)
		{
			Contract.Requires(0 <= i && i <= this.Elements.Count);
			Contract.Requires(!Contains(element));

			List<T> newList = new List<T> { element };
			sortedElements.Insert(i, newList);
			readOnlyElements.Insert(i, newList.ToReadOnlyList());
		}
		/// <summary> Removes the specified element from this comparer. </summary>
		/// <param name="element"> The element to remove. </param>
		/// <returns> whether the element was removed. </returns>
		public bool Remove(T element)
		{
			foreach (List<T> list in this.sortedElements)
				if (list.Remove(element))
				{
					if (list.Count == 0)
						sortedElements.Remove(list);
					return true;
				}
			return false;
		}

		/// <summary> Gets whether this comparer can compare the specified element. </summary>
		public bool Contains(T element)
		{
			return sortedElements.Any(innerList => innerList.Contains(element));
		}
		/// <summary> Compares the two specified element for precedence. </summary>
		public int Compare(T? x, T? y)
		{
			Contract.Requires(ReferenceEquals(x, null) || this.Contains(x));
			Contract.Requires(ReferenceEquals(y, null) || this.Contains(y));
			
			if (ReferenceEquals(x, y)) return 0;
			if (ReferenceEquals(x, null)) return 1;
            if (ReferenceEquals(y, null)) return -1;

			return GetComparisonIndex(x).CompareTo(GetComparisonIndex(y));
		}
		/// <summary> Gets the index of the list in the sorted preferences that contains the specified element. </summary>
		private int GetComparisonIndex(T element)
		{
			Contract.Requires(this.Contains(element));

			return this.sortedElements.FindIndex(innerList => innerList.Contains(element));
		}

	}
}
