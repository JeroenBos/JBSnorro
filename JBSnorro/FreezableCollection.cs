using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	interface IFreezable
	{
		/// <summary> Gets or sets whether this object has been frozen. </summary>
		bool Frozen { get; set; }
	}
	class FreezableList<T> : IList<T>, IFreezable
	{
		/// <summary> The underlying data. </summary>
		private readonly List<T> data;
		/// <summary> Indicates whether this list has been frozen yet. </summary>
		private bool frozen;
		/// <summary> Gets or sets whether this list has been frozen. </summary>
		public bool Frozen
		{
			get { return this.frozen; }
			set
			{
				if (frozen && !value)
					throw new InvalidOperationException("Cannot unfreeze a freezable list");
				this.frozen = value;
			}
		}
		/// <summary> Gets or sets the specified element at the specified index. </summary>
		public T this[int index]
		{
			get
			{
				return data[index];
			}
			set
			{
				if (frozen) throw new FrozenException();
				data[index] = value;
			}
		}
		/// <summary> Gets the number of elements in this list. </summary>
		public int Count
		{
			get { return data.Count; }
		}

		//TODO: implement comments

		public FreezableList()
		{
			this.data = new List<T>();
		}
		public FreezableList(int capacity)
		{
			this.data = new List<T>(capacity);
		}
		public FreezableList(IEnumerable<T> collection)
		{
			this.data = new List<T>(collection);
		}
		public void Add(T item)
		{
			if (frozen) throw new FrozenException();
			data.Add(item);
		}
		public void Clear()
		{
			if (frozen) throw new FrozenException();
			data.Clear();
		}
		public bool Contains(T item)
		{
			return data.Contains(item);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			data.CopyTo(array, arrayIndex);
		}
		public int IndexOf(T item)
		{
			return data.IndexOf(item);
		}
		public void Insert(int index, T item)
		{
			if (frozen) throw new FrozenException();
			data.Insert(index, item);
		}
		public bool IsReadOnly
		{
			get { return frozen; }
		}
		public bool Remove(T item)
		{
			if (frozen) throw new FrozenException();
			return data.Remove(item);
		}
		public void RemoveAt(int index)
		{
			if (frozen) throw new FrozenException();

			data.RemoveAt(index);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return data.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return data.GetEnumerator();
		}

		private sealed class FrozenException : InvalidOperationException
		{
			public FrozenException() : base("The collection is frozen and cannot be modified") { }
		}
	}
}
