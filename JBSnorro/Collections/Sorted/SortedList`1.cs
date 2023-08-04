using JBSnorro;
using JBSnorro.Algorithms;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace JBSnorro.Collections.Sorted;

public class SortedList<T> : IList<T>, ISortedList<T>, IList, ISortedReadOnlyList<T>
{
	/// <summary> Creates a sorted list containing the specified (not necessarily already sorted) initial data. </summary>
	public static SortedList<T> Create(IEnumerable<T> sequence, Func<T, T, int>? comparer = null)
	{
		Contract.Requires(sequence != null);

		comparer = comparer.OrDefault();
		return new SortedList<T>(sequence.OrderBy(comparer).ToList(), comparer);
	}

	/// <summary> The underlying data structure. </summary>
	private readonly IList<T> data;
	/// <summary> Gets the comparer against which this list is sorted. </summary>
	public Func<T, T, int> Comparer { get; private set; }
	/// <summary> Gets or sets the value in this list at the specified index. </summary>
	/// <param name="index"> The index of the element to get. In case of set, the correct index must be provided to have this list remain sorted. </param>
	[DebuggerHidden]
	public T this[int index]
	{
		get { return data[index]; }
		set
		{
			Contract.Requires(0 <= index);
			Contract.Requires(index < Count);
			if (!ComparesAt(value, index))
				throw new InvalidOperationException("Can't place element here: resulting list would be unsorted");

			data[index] = value;
		}
	}

	/// <summary> Creates a new empty sorted list, with the specified comparer and initial capacity. </summary>
	/// <param name="comparer"> The comparer to sort against. Specify null to use the default comparer. </param>
	/// <param name="initialCapacity"> The initial capacity of this list. </param>
	[DebuggerHidden]
	public SortedList(Func<T, T, int>? comparer = null, int initialCapacity = 4)
	{
		this.Comparer = comparer.OrDefault();
		this.data = new List<T>(initialCapacity);
	}

	/// <summary> Creates a new empty sorted list, with the specified comparer and initial capacity. </summary>
	/// <param name="initialData"> This list is used by reference! The initial elements of this sorted list. Must already be sorted. </param>
	/// <param name="comparer"> The comparer to sort against. Specify null to use the default comparer. </param>
	[DebuggerHidden]
	public SortedList(IList<T> initialData, Func<T, T, int>? comparer = null)
	{
		Contract.Requires(initialData != null);
		Contract.Requires(initialData.IsSorted(comparer.OrDefault()));

		this.Comparer = comparer.OrDefault();
		this.data = initialData;

		Contract.Assert(this.IsSorted(Comparer));
	}


	/// <summary> Inserts the specified item into this sorted list in order and returns the index at which the item was placed. </summary>
	/// <param name="item"> The item to add. </param>
	public int Add(T item)
	{
		this.data.Add(item);
		return this.Resort(this.Count - 1);
	}
	/// <summary> Inserts the specified item at the specified index, asserting that the list remains sorted. </summary>
	/// <param name="index"> The index to place the specified item at. Throws if the list would become unsorted due to the insertion. </param>
	/// <param name="item"> The item to insert. </param>
	public void Insert(int index, T item)
	{
		Contract.Requires(0 <= index);
		Contract.Requires(index <= this.Count);

		if (!ComparesBefore(item, index))
			throw new ArgumentException("index is wrong: result would be unsorted");

		this.data.Insert(index, item);
	}
	/// <summary> Inserts the specified items in this list and ensures it remains sorted. </summary>
	/// <param name="items"> The items to insert. </param>
	public void AddRange(IEnumerable<T> items)
	{
		Contract.Requires(items != null);

		foreach (var item in items)
		{
			this.Add(item);
		}
	}

	/// <summary> Informs this list that the element at the specified index may have changed and that this list may need to reorder that element. </summary>
	/// <param name="index"> The index of the element that may have changed. </param>
	/// <returns> the index of the new position of the element. </returns>
	public int OnValueChanged(int index)
	{
		return this.Resort(index);
	}
	/// <summary> Replaces the specified old element in this list with the new element and reorders it if necessary. </summary>
	/// <param name="oldValue"> The element that is to be changed. </param>
	/// <param name="newValue"> The new value of the element to be changed.</param>
	/// <returns> the index of the new position of the element. </returns>
	public int Change(T oldValue, T newValue)
	{
		int index = IndexOf(oldValue);
		if (index == -1)
			throw new KeyNotFoundException("The specified old value was not present in this list");
		return Change(index, newValue);
	}
	/// <summary> Replaces the element at the specified index with the new element and reorders it if necessary. </summary>
	/// <param name="index"> The index of the element that is to be changed. </param>
	/// <param name="newValue"> The new value of the element to be changed.</param>
	/// <returns> the index of the new position of the element. </returns>
	public int Change(int index, T newValue)
	{
		Contract.Requires(0 <= index && index < this.Count);

		data[index] = newValue;
		return Resort(index);
	}

	/// <summary> Resorts this collection, assuming that only the element at the specified index is not sorted. </summary>
	/// <param name="index"> The index of the element to sort. </param>
	private int Resort(int index)
	{
		Contract.Requires(0 <= index);
		Contract.Requires(index < this.Count);
		Contract.Requires(this.ExceptAt(index).IsSorted(this.Comparer));

		T itemToResort = this[index];
		int indexToPlace = this.IndexOf(itemToResort, ignoreIndex: index, comparer: this.Comparer);

		if (indexToPlace > index)
		{
			for (int i = index; i < indexToPlace; i++)
			{
				this[i] = this[i + 1];
			}
		}
		else
		{
			for (int i = index; i > indexToPlace; i--)
			{
				this[i] = this[i - 1];
			}
		}
		this[indexToPlace] = itemToResort;
		return indexToPlace;
	}
	/// <summary> Returns whether the specified item compares between the element before the specified index and at the specified index, if any. </summary>
	/// <param name="index"> The index just before which insertion is checked for order. </param>
	/// <param name="item"> The item to check for comparison between. </param>
	private bool ComparesBefore(T item, int index)
	{
		Option<T> min = index == 0 ? Option<T>.None : this[index - 1];
		Option<T> max = index >= Count ? Option<T>.None : this[index];

		return ComparesBetween(item, min, max);
	}
	/// <summary> Returns whether the specified item compares between the element before the specified index and after the specified index, if any. </summary>
	/// <param name="index"> The index at which insertion is checked for order. </param>
	/// <param name="item"> The item to check for comparison between. </param>
	private bool ComparesAt(T item, int index)
	{
		Option<T> min = index == 0 ? Option<T>.None : this[index - 1];
		Option<T> max = index >= Count - 1 ? Option<T>.None : this[index + 1];

		return ComparesBetween(item, min, max);
	}
	/// <summary> Gets whether the specified item compares between the specified min and max, if any. </summary>
	private bool ComparesBetween(T item, Option<T> min, Option<T> max)
	{
		if (min.HasValue && Comparer(item, min.Value) < 0)
			return false;
		if (max.HasValue && Comparer(item, max.Value) > 0)
			return false;
		return true;
	}

	#region Trivial Members


	void ICollection<T>.Add(T item)
	{
		this.Add(item);
	}

	/// <summary> Removes all elements from this list. </summary>
	public void Clear()
	{
		this.data.Clear();
	}
	/// <summary> Gets whether this list contains the specified item. </summary>
	public bool Contains(T item)
	{
		return this.IndexOf(item) != -1;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		data.CopyTo(array, arrayIndex);
	}


	/// <summary> Gets the number of elements in this list. </summary>
	public int Count
	{
		get { return this.data.Count; }
	}

	public bool IsReadOnly
	{
		get { return false; }
	}

	bool IList.IsReadOnly
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	bool IList.IsFixedSize
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	int ICollection.Count => data.Count;

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	bool ICollection.IsSynchronized
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	object? IList.this[int index]
	{
		get
		{
			throw new NotImplementedException();
		}

		set
		{
			throw new NotImplementedException();
		}
	}

	/// <summary> Gets the index in this list of specified item, or -1 if it wasn't found. </summary>
	/// <param name="item"> The item to get the index of. </param>
	public int IndexOf(T item)
	{
		return BinarySearch.IndexOf(this, item, Comparer);
	}



	/// <summary> Removes the specified object from this collection. </summary>
	/// <param name="item">The object to remove from the collection. </param>
	/// <returns> true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>. </returns>
	public bool Remove(T item)
	{
		return this.data.Remove(item);
	}
	/// <summary> Removes the element in this list at the specified index. </summary>
	/// <param name="index"> The index of the element to remove. </param>
	public void RemoveAt(int index)
	{
		this.data.RemoveAt(index);
	}

	int IList.Add(object? value)
	{
		Contract.Requires(value is T);

		return Add((T)value);
	}
	bool IList.Contains(object? value)
	{
		Contract.Requires(value is T);

		return Contains((T)value);
	}
	int IList.IndexOf(object? value)
	{
		Contract.Requires(value is T);

		return IndexOf((T)value);
	}
	void IList.Insert(int index, object? value)
	{
		Contract.Requires(value is T);

		Insert(index, (T)value);
	}
	void IList.Remove(object? value)
	{
		Contract.Requires(value is T);

		Remove((T)value);
	}
	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)data).CopyTo(array, index);
	}

	[DebuggerHidden]
	public IEnumerator<T> GetEnumerator()
	{
		return data.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	#endregion

	public void TakeWhile(Func<int, int, bool> func)
	{
		throw new NotImplementedException();
	}


	/// <summary> Gets whether this list contains an element between the two specified elements. </summary>
	/// <param name="min"> The lowest item of the two between which it is to be determined if there is an element. 
	/// This item need not be an element of this list, but merely be comparable to the list elements. 
	/// The items itself is considered outside of the range to find an element in. </param>
	/// <param name="max"> The highest item of the two between which it is to be determined if there is an element. 
	/// This item need not be an element of this list, but merely be comparable to the list elements. 
	/// The items itself is considered outside of the range to find an element in. </param>
	public bool ContainsElementBetween(T min, T max)
	{
		int minPosition = BinarySearch.IndexPositionOf(i => this[i], this.Count, min, this.Comparer);
		int maxPosition = BinarySearch.IndexPositionOf(i => this[i], this.Count, max, this.Comparer);
		return minPosition < maxPosition;
	}
}
