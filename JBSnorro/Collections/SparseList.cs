using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> Represents a list but underlying it is a dictionary mapping indices to list elements. </summary>
	public class SparseList<T> : IList<T>
	{
		/// <summary> The underlying data structure. </summary>
		private readonly SortedDictionary<int, T> items;
		/// <summary> Gets the default value yielded when no value is presented at a specified index. </summary>
		public T DefaultValue { get; }
		/// <summary> Gets the equality comparer used among the contained items and any specified item (in the methods 'Contains', 'Remove' and 'IndexOf'). </summary>
		public IEqualityComparer<T> EqualityComparer { get; }
		public SparseList(T defaultValue = default(T), IEqualityComparer<T> equalityComparer = null)
		{
			this.items = new SortedDictionary<int, T>();
			this.DefaultValue = defaultValue;
			this.EqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return items.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		void ICollection<T>.Add(T item)
		{
			throw new InvalidOperationException();
		}
		public void Clear()
		{
			items.Clear();
		}
		public bool Contains(T item)
		{
			return items.Values.Contains(item, EqualityComparer);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			items.Values.CopyTo(array, arrayIndex);
		}
		public bool Remove(T item)
		{
			int key = this.IndexOf(item);
			if (key == -1)
				return false;
			return items.Remove(key);
		}
		public int Count
		{
			get { return items.Count; }
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public int IndexOf(T item)
		{
			return items.IndexOf(kvp => this.EqualityComparer.Equals(kvp.Value, item));
		}
		void IList<T>.Insert(int index, T item)
		{
			throw new InvalidOperationException();// I could increment all indices in the dictionary by one and just insert this element as normal...
		}
		public void RemoveAt(int index)
		{
			bool success = items.Remove(index);
			if (!success) throw new IndexOutOfRangeException();
		}
		T IList<T>.this[int index]
		{
			[DebuggerHidden]
			get
			{
				var result = this[index];
				if (result.HasValue)
					return result.Value;
				return DefaultValue;
			}
			[DebuggerHidden]
			set
			{
				this[index] = value;
			}
		}
		public Option<T> this[int index]
		{
			get
			{
				T result;
				if (items.TryGetValue(index, out result))
					return result;
				return default(Option<T>);
			}
			set
			{
				if (value.HasValue)
					items[index] = value.Value;
				else
					items.Remove(index);
			}
		}
	}
}
