using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary>
	/// Represents a list of limited size. 
	/// </summary>
	public class LimitedList<T> : IList<T>, IList
	{
		/// <summary>
		/// The index in <see cref="_data"/> of the first element in this list. 
		/// </summary>
		private int pointer;
		private readonly T[] _data;

		public int Capacity => _data.Length;
		public int Count { get; private set; }

		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException();
				return _data[(pointer + index) % _data.Length];
			}
			set
			{
				if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException();
				_data[(pointer + index) % _data.Length] = value;
			}
		}

		public LimitedList(int capacity)
		{
			this._data = new T[capacity];
		}
		public LimitedList(IEnumerable<T> initialElements)
		{
			this._data = initialElements.ToArray();
		}
		public LimitedList(int capacity, IEnumerable<T> initialElements)
			: this(capacity)
		{
			foreach (var initialElement in initialElements)
				this.Add(initialElement);
		}

		public void Add(T item)
		{
			// order of operations matters
			this[this.Count] = item;
			if (this.Count == this.Capacity)
			{
				pointer++;
			}
			this.Count++;
		}

		public void Prepend(T item)
		{
			// order of operations matters
			if (this.Count != this.Capacity)
			{
				this.Count++;
			}
			pointer = (pointer + this.Count - 1) % this._data.Length;
			this[0] = item;
		}
		public void Clear()
		{
			this.Count = 0;
		}

		public bool Contains(T item)
		{
			return this.Contains<T>(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Enumerable.Range(0, this.Count)
							 .Select(i => this[i])
							 .GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return this.IndexOf<T>(item);
		}

		public void Insert(int index, T item)
		{
			//design choice: the one at the top of the list is kicked out in case it overflows.
			T last = this.LastOrDefault();

			for (int i = this.Count - 2; i >= index; i--)
			{
				this[i + 1] = this[i];
			}
			this[index] = item;

			if (Capacity != Count)
			{
				this.Add(last);
			}
		}

		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public void RemoveAt(int index)
		{
			for (int i = index; i < this.Count - 1; i++)
			{
				this[i] = this[i + 1];
			}
			this.Count--;
		}


		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}
		bool ICollection<T>.IsReadOnly => false;

		#region IList Members 

		int IList.Add(object value)
		{
			this.Add((T)value);
			return this.Count;
		}
		bool IList.Contains(object value)
		{
			if (value is T t)
				return this.Contains(t);
			return false;
		}
		int IList.IndexOf(object value)
		{
			if (value is T t)
				return this.IndexOf(t);
			return -1;
		}
		void IList.Insert(int index, object value)
		{
			this.Insert(index, (T)value);
		}
		void IList.Remove(object value)
		{
			if (value is T t)
				this.Remove(t);
		}
		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			throw new NotImplementedException();
		}
		bool IList.IsReadOnly => throw new NotImplementedException();
		bool IList.IsFixedSize => false;
		object ICollection.SyncRoot => throw new NotImplementedException();
		bool ICollection.IsSynchronized => throw new NotImplementedException();

		object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion
	}
}
