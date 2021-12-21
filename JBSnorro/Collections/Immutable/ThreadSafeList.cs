using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBSnorro.Collections.Immutable
{
	/// <summary>
	/// Represents a list that can mutate only by atomic operations.
	/// </summary>
	public class ThreadSafeList<T> where T : class
	{
		private ImmutableList<T> data = ImmutableList.Create<T>();
		private ImmutableList<T> update(Func<ImmutableList<T>> value)
		{
			ImmutableList<T> oldData, setData;
			do
			{
				var newData = value();
				if (newData == null) { throw new ArgumentException($"'{nameof(value)}' may not return null. "); }

				oldData = this.data;
				setData = Interlocked.Exchange(ref this.data, newData);
			}
			while (oldData != setData);
			return oldData;
		}
		/// <summary>
		/// Clears the current list and returns the values it had at that moment.
		/// </summary>
		public IReadOnlyList<T> Clear()
		{
			return update(ImmutableList.Create<T>);
		}
		public void Add(T item)
		{
			this.update(() => this.data.Add(item));
		}

		public void AddRange(IEnumerable<T> items)
		{
			this.update(() => this.data.AddRange(items));
		}

		public int Count => this.data.Count;
	}
}
