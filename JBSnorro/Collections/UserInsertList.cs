using JBSnorro.Diagnostics;
using System;
using System.Collections;

namespace JBSnorro.Collections;

/// <summary> This type represents a collection which may be exposed such that the user may only view the elements and insert elements, but not set or remove elements. 
/// A predicate delegate filters items on whether they are allowed to be inserted into this list. </summary>
public sealed class UserInsertList<T> : IList<T>
{
	/// <summary> The function that determines whether an item would be accepted to be inserted in or added to this list. </summary>
	private readonly Action<T, int> onInsert;
	/// <summary> The underlying data structure. </summary>
	
	private readonly IList<T> data;
	/// <summary> Gets whether any elements have been inserted or added to this list since its construction. </summary>
	public bool Changed { get; private set; }

	/// <summary> Gets the elements at the specified index. </summary>
	public T this[int index]
	{
		get { return this.data[index]; }
		set { throw new InvalidOperationException("A UserInsertList does not allow setting elements"); }
	}

	/// <summary> Creates a new user insert list for the specified underlying data structure. </summary>
	/// <param name="data"> The underlying data structure to which the user will be able to insert or add elements directly. Cannot be null. </param>
	/// <param name="onInsert"> A function that is called just before an item is inserted. This can be used for instance for validating the item to be inserted.
	/// Specifying null performs nothing just before insertion. The arguments are the item to be inserted and the index at which it is to be inserted. </param>
	public UserInsertList( IList<T> data, Action<T, int>? onInsert = null)
	{
		Contract.Requires(data != null);
		this.data = data;
		this.onInsert = onInsert;
	}

	/// <summary> Adds the specified item to the end of this list if it matches the predicate specified to this list at construction. </summary>
	/// <param name="item"> The item to add to this list. </param>
	public void Add(T item)
	{
		if (onInsert != null)
			onInsert(item, this.Count);
		data.Add(item);
		this.Changed = true;
	}
	/// <summary> Inserts an item at the specified index if it matches the predicate specified to this list at construction. </summary> 
	/// <param name="index"> The index at which to insert. </param>
	/// <param name="item"> The item to insert. </param>
	public void Insert(int index, T item)
	{
		if (onInsert != null)
			onInsert(item, index);
		this.data.Insert(index, item);
		this.Changed = true;
	}

	#region Trivial Members
	/// <summary> Determines whether this list contains a specific value. </summary>
	/// <param name="item"> The object to locate in this list.</param>
	public bool Contains(T item)
	{
		return this.data.Contains(item);
	}
	/// <summary> Copies the contents of this list to the specified arrary. </summary>
	public void CopyTo(T[] array, int arrayIndex)
	{
		this.data.CopyTo(array, arrayIndex);
	}
	/// <summary> Gets the number of elements in this list, including any inserted elements. </summary>
	public int Count
	{
		get { return this.data.Count; }
	}
	/// <summary> Gets false, this list is mutable. </summary>
	public bool IsReadOnly
	{
		get { return false; }
	}
	/// <summary> Returns the index of the specified item in this list, or -1 if it was not found. </summary>
	public int IndexOf(T item)
	{
		return this.data.IndexOf(item);
	}

	/// <summary> Gets the enumerator of the underlying list. </summary>
	public IEnumerator<T> GetEnumerator()
	{
		return data.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	#endregion
	#region Not supported Members
	bool ICollection<T>.Remove(T item)
	{
		throw new InvalidOperationException("A UserInsertList does not allow removing elements");
	}
	void IList<T>.RemoveAt(int index)
	{
		throw new InvalidOperationException("A UserInsertList does not allow removing elements");
	}
	void ICollection<T>.Clear()
	{
		throw new InvalidOperationException("A UserInsertList does not allow removing elements");
	}
	#endregion
}
