#nullable disable
using JBSnorro.BackingFieldEnforcer;
using JBSnorro.Collections.ObjectModel;
using JBSnorro.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace JBSnorro;

public class ConcatenatedReadOnlyObservableCollection<T> : DefaultINotifyPropertyChanged, INotifyCollectionChanged, IReadOnlyList<T>
{
	[BackingField]
	private int count;
	private readonly ObservableCollection<T>[] collections;

	public event NotifyCollectionChangedEventHandler CollectionChanged;


	public int Count
	{
		get { return count; }
		set { Set(ref count, value); }
	}
	public T this[int index]
	{
		get
		{
			Contract.Requires(0 <= index && index < this.Count);

			foreach (var underlyingCOllection in this.collections)
			{
				if (index < underlyingCOllection.Count)
				{
					return underlyingCOllection[index];
				}
				index -= underlyingCOllection.Count;
			}

			throw new DefaultSwitchCaseUnreachableException();
		}
	}


	public ConcatenatedReadOnlyObservableCollection(params ObservableCollection<T>[] collections)
	{
		Contract.Requires(collections != null);
		Contract.RequiresForAll(collections, Global.NotNull);

		this.collections = collections;
		foreach (var collection in collections)
		{
			collection.CollectionChanged += onCollectionCollectionChanged;
		}
		this.recomputeCount();
	}

	private void onCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		Contract.Requires(sender is ObservableCollection<T>);
		Contract.Requires(this.collections.Contains(sender));
		Contract.Requires(e != null);

		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				this.Count += e.NewItems.Count;
				break;
			case NotifyCollectionChangedAction.Remove:
				this.Count -= e.OldItems.Count;
				break;
			case NotifyCollectionChangedAction.Replace:
				this.Count += e.NewItems.Count - e.NewItems.Count;
				break;
			case NotifyCollectionChangedAction.Move:
				break;
			case NotifyCollectionChangedAction.Reset:
				recomputeCount(); // I don't think one can reason what the new count is going to be in this case. Just recomputing is the only solution
				break;
			default:
				throw new DefaultSwitchCaseUnreachableException();
		}

		int startIndexOfSender = computeStartingIndex((ObservableCollection<T>)sender);
		OnCollectionChanged(e.Shift(startIndexOfSender));
	}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		this.CollectionChanged?.Invoke(this, e);
	}
	/// <summary> Gets the index in this collection where the specified underlying collection starts. </summary>
	private int computeStartingIndex(ObservableCollection<T> collection)
	{
		Contract.Requires(collection != null);
		Contract.Requires(this.collections.Contains(collection));

		return this.collections.TakeWhile(underlyingCollection => underlyingCollection != collection)
				   .Sum(underlyingCollection => underlyingCollection.Count);
	}
	private void recomputeCount()
	{
		this.Count = this.collections.Sum(collection => collection.Count);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return collections.Concat().GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
