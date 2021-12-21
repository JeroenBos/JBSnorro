using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace JBSnorro
{
	/// <summary>
	/// Provides a wrapper around a collection of delegates associated to the <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	public class CollectionChangedEventHandler<T>
	{
		/// <summary>
		/// Creates a <see cref="CollectionChangedEventHandler{T}"/> from the specified delegates.
		/// </summary>
		/// <param name="add"> If null is specified, nothing is performed on addition of an element. </param>
		/// <param name="move"> If null is specified, nothing is performed on removal of an element. </param>
		/// <param name="remove"> If null is specified, nothing is performed on removal of an element. </param>
		/// <param name="replace"> If null is specified, the default action is performed, which is to first remove and then add the changes. </param>
		/// <param name="reset"> If null is specified, the default action is performed, which is to remove all elements. </param>
		public static CollectionChangedEventHandler<T> Create<TCollection>(
			AddDelegate<TCollection> add = null,
			RemoveDelegate<TCollection> remove = null,
			ReplaceDelegate<TCollection> replace = null,
			MoveDelegate<TCollection> move = null,
			ResetDelegate<TCollection> reset = null
			) where TCollection : INotifyCollectionChanged
		{
			return new DelegatedCollectionChangedEventHandler<TCollection>(move, add, reset, replace, remove);
		}
		public virtual void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Contract.Requires(sender is INotifyCollectionChanged);
			Contract.Requires(e != null);
			var collection = (INotifyCollectionChanged)sender;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				{
					var newElements = asList(e.NewItems);
					int index = e.NewStartingIndex;
					this.Add(collection, newElements, index);
					break;
				}
				case NotifyCollectionChangedAction.Move:
				{
					var elements = asList(e.NewItems);
					int fromIndex = e.OldStartingIndex;
					int toIndex = e.NewStartingIndex;
					this.Move(collection, elements, toIndex, fromIndex);
					break;
				}
				case NotifyCollectionChangedAction.Remove:
				{
					var oldElements = asList(e.OldItems);
					int index = e.OldStartingIndex;
					this.Remove(collection, oldElements, index);
					break;
				}
				case NotifyCollectionChangedAction.Replace:
				{
					var oldElements = asList(e.OldItems);
					var newElements = asList(e.NewItems);
					int index = e.NewStartingIndex;
					this.Replace(collection, newElements, oldElements, index);
					break;
				}
				case NotifyCollectionChangedAction.Reset:
				{
					var oldElements = e.OldItems.Cast<T>().ToArray();
					this.Reset(collection, oldElements);
					break;
				}
				default:
					throw new ArgumentException("e.Action");
			}

			IReadOnlyList<T> asList(System.Collections.IList list)
			{
				return (list as IReadOnlyList<T>) ?? list.Cast<T>().ToList();
			}
		}
		/// <summary>
		/// Handles the invocation of <see cref="NotifyCollectionChangedAction.Add"/>.
		/// </summary>
		protected virtual void Add(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, int index)
		{
		}
		/// <summary>
		/// Handles the invocation of <see cref="NotifyCollectionChangedAction.Move"/>.
		/// </summary>
		protected virtual void Move(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, int index, int fromIndex)
		{
		}
		/// <summary>
		/// Handles the invocation of <see cref="NotifyCollectionChangedAction.Remove"/>.
		/// </summary>
		protected virtual void Remove(INotifyCollectionChanged collection, IReadOnlyList<T> oldElements, int index)
		{
		}
		/// <summary>
		/// Handles the invocation of <see cref="NotifyCollectionChangedAction.Reset"/>.
		/// By default calls <code>Remove(collection, elements, 0);</code>
		/// </summary>
		protected virtual void Reset(INotifyCollectionChanged collection, IReadOnlyList<T> oldElements)
		{
			Remove(collection, oldElements, 0);
		}
		/// <summary>
		/// Handles the invocation of <see cref="NotifyCollectionChangedAction.Replace"/>.
		/// </summary>
		protected virtual void Replace(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, IReadOnlyList<T> oldElements, int index)
		{
		}

		public delegate void AddDelegate<TCollection>(TCollection collection, IReadOnlyList<T> newElements, int index);
		public delegate void MoveDelegate<TCollection>(TCollection collection, IReadOnlyList<T> elements, int toIndex, int fromIndex);
		public delegate void ResetDelegate<TCollection>(TCollection collection, IReadOnlyList<T> elements);
		public delegate void ReplaceDelegate<TCollection>(TCollection collection, IReadOnlyList<T> newElements, IReadOnlyList<T> oldElements, int index);
		public delegate void RemoveDelegate<TCollection>(TCollection collection, IReadOnlyList<T> oldElements, int index);

		private sealed class DelegatedCollectionChangedEventHandler<TCollection> : CollectionChangedEventHandler<T>
		{
			private readonly AddDelegate<TCollection> add;
			private readonly MoveDelegate<TCollection> move;
			private readonly ResetDelegate<TCollection> reset;
			private readonly ReplaceDelegate<TCollection> replace;
			private readonly RemoveDelegate<TCollection> remove;

			public DelegatedCollectionChangedEventHandler(
				MoveDelegate<TCollection> move,
				AddDelegate<TCollection> add,
				ResetDelegate<TCollection> reset,
				ReplaceDelegate<TCollection> replace,
				RemoveDelegate<TCollection> remove
				)
			{
				this.move = move;
				this.add = add;
				this.reset = reset;
				this.replace = replace;
				this.remove = remove;
			}
			protected override void Move(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, int index, int fromIndex)
			{
				this.move?.Invoke((TCollection)collection, newElements, index, fromIndex);
			}
			protected override void Remove(INotifyCollectionChanged collection, IReadOnlyList<T> oldElements, int index)
			{
				this.remove?.Invoke((TCollection)collection, oldElements, index);
			}
			protected override void Replace(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, IReadOnlyList<T> oldElements, int index)
			{
				this.replace?.Invoke((TCollection)collection, newElements, oldElements, index);
			}
			protected override void Reset(INotifyCollectionChanged collection, IReadOnlyList<T> oldElements)
			{
				this.reset?.Invoke((TCollection)collection, oldElements);
			}
			protected override void Add(INotifyCollectionChanged collection, IReadOnlyList<T> newElements, int index)
			{
				this.add?.Invoke((TCollection)collection, newElements, index);
			}
		}
	}
}
