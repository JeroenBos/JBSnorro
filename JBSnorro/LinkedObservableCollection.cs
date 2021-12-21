using JBSnorro.BackingFieldEnforcer;
using JBSnorro.Collections.ObjectModel;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace JBSnorro
{
	/// <summary> Represents a live map of another observable collection. </summary>
	public class LinkedObservableCollection<T, TResult> : ProperObservableCollection<TResult>, IReadOnlyObservableCollection<TResult>
	{
		/// <summary> Gets or sets the source collection. </summary>
		public INotifyCollectionChanged Source
		{
			get { return source; }
			set
			{
				Contract.Requires(value == null || value is IEnumerable<T>, "The source must implement IEnumerable<T>");

				if (value != this.Source)
				{
					if (this.Source != null)
					{
						this.Source.CollectionChanged -= onSourceCollectionChanged;
					}

					if (value == null)
					{
						this.Clear();
					}
					else
					{
						value.CollectionChanged += onSourceCollectionChanged;
						this.Replace(((IEnumerable<T>)value).Select(map));
					}
					Set(ref source, value);
				}
			}
		}
		private readonly Func<T, LinkedObservableCollection<T, TResult>, TResult> selector;
		private Func<T, TResult> map => (element => selector(element, this));

		/// <summary> Creates a new linked observable collection with the specified link. </summary>
		/// <param name="selector"> The function converting the source to the contents of this collection. </param>
		public LinkedObservableCollection(Func<T, TResult> selector)
		{
			Contract.Requires(selector != null);

			this.selector = (element, @this) => selector(element);
		}

		/// <summary> Creates a new linked observable collection with the specified link. </summary>
		/// <param name="selector"> The function converting the source to the contents of this collection taking a reference to this collection. </param>
		public LinkedObservableCollection(Func<T, LinkedObservableCollection<T, TResult>, TResult> selector)
		{
			Contract.Requires(selector != null);

			this.selector = selector;
		}

		private void onSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewStartingIndex == -1)
					{
						AddRange(e.NewItems.Cast<T>().Select(map));
					}
					else
					{
						InsertRange(e.NewStartingIndex, e.NewItems.Cast<T>().Select(map));
					}
					break;
				case NotifyCollectionChangedAction.Move:
					MoveRange(e.OldStartingIndex, e.NewItems.Count, e.NewStartingIndex);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					break;
				case NotifyCollectionChangedAction.Replace:
					Replace(e.NewStartingIndex, e.OldItems.Count, e.NewItems.Cast<T>().Select(map));
					break;
				case NotifyCollectionChangedAction.Reset:
					this.Clear();
					break;
				default:
					throw new DefaultSwitchCaseUnreachableException();
			}
		}

		[BackingField]
		private INotifyCollectionChanged source;
	}
}
