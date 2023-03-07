using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Represents an array that computes its elements lazily and caches them. </summary>
	public sealed class LazyArray<TSource, TResult> : IReadOnlyList<TResult>, IList<TResult>
	{
		private sealed class Element
		{
			public readonly TResult CachedElement;//if performance required, make Element a sequential struct
			public readonly TSource OriginalElement;

			private Element(TResult cachedElement)
			{
				CachedElement = cachedElement;
			}
			private Element(TSource originalElement)
			{
				OriginalElement = originalElement;
			}

			public static implicit operator Element(TResult a)
			{
				return new Element(a);
			}
			public static implicit operator Element(TSource a)
			{
				return new Element(a);
			}

			public LazyArray<TSource, TResult> owner;//debug member
			public int index;//debug member
			public override string ToString()
			{
				return owner.cached[index] ? "Cached: " + CachedElement : ("Original: " + OriginalElement);
			}
		}

		private readonly List<bool> cached;
		private readonly List<Element> elements;
		private readonly Func<TSource, TResult> resultSelector;
		[DebuggerHidden]
		public IEnumerator<TResult> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
				yield return this[i];
		}

		public void Add(TResult item)
		{
			cached.Add(true);
		}
		public void Clear()
		{
			throw new InvalidOperationException();
		}
		public bool Contains(TResult item)
		{
			return Enumerable.Contains(this, item);
		}
		public void CopyTo(TResult[] array, int arrayIndex)
		{
			for (int i = 0; i < this.Count; i++)
			{
				array[arrayIndex + i] = this[i];
			}
		}
		public bool Remove(TResult item)
		{
			throw new NotImplementedException();
		}
		public int Count
		{
			get { return this.elements.Count; }
		}
		public bool IsReadOnly
		{
			get { throw new InvalidOperationException(); }
		}
		public int IndexOf(TResult item)
		{
			return EnumerableExtensions.IndexOf(this, item);
		}
		public void Insert(int index, TResult item)
		{
			elements.Insert(index, item);
			cached.Insert(index, true);
		}
		public void RemoveAt(int index)
		{
			elements.RemoveAt(index);
			cached.RemoveAt(index);
		}

		public TResult this[int index]
		{
			[DebuggerHidden]
			get
			{
				if (!cached[index])
				{
					elements[index] = resultSelector(elements[index].OriginalElement);
					cached[index] = true;
				}
				return elements[index].CachedElement;
			}
			set
			{
				elements[index] = value;
				cached[index] = true;
			}
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public LazyArray(IReadOnlyList<TSource> source, Func<TSource, TResult> resultSelector)
		{
			this.elements = new List<Element>();
			foreach (TSource item in source)
			{
				this.elements.Add((Element)item);
			}
			cached = new List<bool>(elements.Count);
			for (int i = 0; i < elements.Count; i++)
			{
				elements[i].index = i;//debug member
				elements[i].owner = this;//debug member
				cached.Add(false);
			}
			this.resultSelector = resultSelector;
		}
	}
}
