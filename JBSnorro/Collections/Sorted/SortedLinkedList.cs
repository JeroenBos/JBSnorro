using JBSnorro.Diagnostics;
using System.Collections;

namespace JBSnorro.Collections.Sorted;

/// <summary> A sorted singly-linked list. </summary>
public class SortedLinkedList<T> : ISortedList<T>
{
	private Node? first;
	/// <summary> The comparer against which this list is sorted. </summary>
	public Func<T, T, int> Comparer { get; }
	/// <summary> The number of elements in this list. </summary>
	public int Count { get; private set; }


	public SortedLinkedList(Func<T, T, int>? comparer = null)
	{
		this.Comparer = comparer.OrDefault();
	}

	public void AddRange(params T[] items)
	{
		foreach (T item in items)
			Add(item);
	}
	public void Add(T item)
	{
		if (first == null)
		{
			first = CreateNode(item, null);
		}
		else if (Comparer(item, first.Value) <= 0)
		{
			this.first = CreateNode(item, this.first);
		}
		else
		{
			var nodeBeforeNewNode = this.first;
			while (nodeBeforeNewNode.Next != null && Comparer(nodeBeforeNewNode.Next.Value, item) <= 0)
			{
				nodeBeforeNewNode = nodeBeforeNewNode.Next;
			}

			Contract.Assert(Comparer(nodeBeforeNewNode.Value, item) <= 0);
			Contract.Assert(nodeBeforeNewNode.Next == null || Comparer(nodeBeforeNewNode.Next.Value, item) > 0);

			nodeBeforeNewNode.Append(item);
		}

		Count++;
	}

	/// <summary> Removes the specified item from this list. </summary>
	/// <returns> whether the item was removed. </returns>
	public bool Remove(T item)
	{
		bool removed;
		if (first == null)
		{
			removed = false;//list contains no elements, so item not found
		}
		else if (Comparer(first.Value, item) == 0)
		{
			first = first.Next;
			removed = true;
		}
		else
		{
			var nodeBeforeNodeToRemove = this.first;
			while (nodeBeforeNodeToRemove.Next != null && Comparer(nodeBeforeNodeToRemove.Next.Value, item) != 0)
			{
				nodeBeforeNodeToRemove = nodeBeforeNodeToRemove.Next;
			}

			if (nodeBeforeNodeToRemove.Next == null)
			{
				removed = false;//item not found
			}
			else
			{
				nodeBeforeNodeToRemove.RemoveNodeAfterThisOne();
				removed = true;
			}
		}

		if (removed)
			Count--;
		return removed;
	}

	protected virtual Node CreateNode(T value, Node? next)
	{
		var result = new Node(this, value, next);

		Contract.Ensures(result != null);
		Contract.Ensures(result.List == this, "The specified node does not belong to this list. ");
		return result;
	}



	T IIndexable<T>.this[int index]
	{
		get
		{
			Contract.Requires(0 <= index && index < Count);
			// if (index < Count / 2) return this.First.Skip(index).First();
			return this.Skip(Count - index).First();
		}
	}
	/// <summary>
	/// Gets the values of this sorted linked list in ascending order.
	/// </summary>
	public IEnumerator<T> GetEnumerator()
	{
		var node = this.first;
		while (node != null)
		{
			yield return node.Value;
			node = node.Next;
		}
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	protected class Node
	{
		/// <summary> The value this node holds. </summary>
		public T Value { get; }
		/// <summary> The node after this node in the associated list. </summary>
		public Node? Next { get; private set; }
		/// <summary> This linked list this node is part of. </summary>
		public SortedLinkedList<T> List { get; }


		internal Node(SortedLinkedList<T> list, T value, Node? next)
		{
			Contract.Requires(list != null);

			this.List = list;
			this.Value = value;
			this.Next = next;
		}


		/// <summary> Adds a new node to the associated list just after the current node holding the specified value. </summary>
		internal void Append(T value)
		{
			Contract.Requires(this.Next == null || List.Comparer(this.Next.Value, value) >= 0, "Appending the specified value would violate sortedness");

			this.Next = this.List.CreateNode(value, this.Next);
			//list count is updated in caller. node.Count is always recomputed so remains correct
		}
		/// <summary> Removes the node after this node. </summary>
		internal void RemoveNodeAfterThisOne()
		{
			Contract.Requires<InvalidOperationException>(this.Next != null, "There is no element after this element to remove. ");

			this.Next = this.Next.Next;
		}
	}
}
