using JBSnorro.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace JBSnorro.Collections.ObjectModel;

public static class ObservableCollectionExtensions
{
	/// <summary> Returns an identical event args but with all index property shifted by the specified amount. </summary>
	public static NotifyCollectionChangedEventArgs Shift(this NotifyCollectionChangedEventArgs e, int delta)
	{
		const int indexUnknown = -1;
		Contract.Requires(e != null);
		Contract.Requires(!(e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == indexUnknown), $"The index '{indexUnknown.ToString()}' denoting 'at the end' cannot be shifted");

		// my understanding of NotifyCollectionChangedEventArgs is limited, hence the assertions. WPF doesn't do documentation... or at least not where I'm looking
		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				Contract.Assert(e.OldItems == null);
				Contract.Assert(e.OldStartingIndex == indexUnknown);

				return new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.NewStartingIndex);
			case NotifyCollectionChangedAction.Remove:
				Contract.Assert(e.NewItems == null);
				Contract.Assert(e.NewStartingIndex == indexUnknown);

				return new NotifyCollectionChangedEventArgs(e.Action, e.OldItems, e.OldStartingIndex);
			case NotifyCollectionChangedAction.Replace:
			case NotifyCollectionChangedAction.Move:
				Contract.Assert(e.OldItems == null);

				return new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.NewStartingIndex, e.OldStartingIndex);
			case NotifyCollectionChangedAction.Reset:
				Contract.Assert(e.OldItems == null);
				Contract.Assert(e.NewItems == null);
				Contract.Assert(e.NewStartingIndex == indexUnknown);
				Contract.Assert(e.OldStartingIndex == indexUnknown);

				return e;
			default:
				throw new DefaultSwitchCaseUnreachableException();
		}
	}

	public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> sequence)
	{
		Contract.Requires(sequence != null);

		return new ObservableCollection<T>(sequence);
	}
	public static MyReadOnlyObservableCollection<T> ToObservableReadOnlyCollection<T>(this IEnumerable<T> sequence)
	{
		Contract.Requires(sequence != null);

		return new MyReadOnlyObservableCollection<T>(sequence);
	}
	public static MyReadOnlyObservableCollection<TResult> SelectLive<T, TResult>(this ObservableCollection<T> collection, Func<T, TResult> selector)
		=> new MyReadOnlyObservableCollection<T>(collection).SelectLive(selector);
	/// <summary>
	/// Creates an observable collection whose content reflects the content of the specified collection, mapped by the specified selector.
	/// This collection is kept up-to-date with respect to changes in the original colletion.
	/// </summary>
	public static MyReadOnlyObservableCollection<TResult> SelectLive<T, TResult>(this IReadOnlyObservableCollection<T> collection, Func<T, TResult> selector)
		=> collection.WhereSelectLive(_ => true, selector);
	/// <summary>
	/// Creates an observable collection whose content reflects the content of the specified collection, filtered by the specified predicate.
	/// This collection is kept up-to-date with respect to changes in the original colletion.
	/// </summary>
	public static MyReadOnlyObservableCollection<T> WhereLive<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
	{
		return collection.WhereSelectLive(predicate, _ => _);
	}
	/// <summary>
	/// Creates an observable collection whose content reflects the content of the specified collection, filtered by the specified predicate and mapped by the specified selector.
	/// This collection is kept up-to-date with respect to changes in the original colletion.
	/// </summary>
	public static MyReadOnlyObservableCollection<TResult> WhereSelectLive<T, TResult>(this ObservableCollection<T> collection, Func<T, bool> predicate, Func<T, TResult> selector)
	{
		return new MyReadOnlyObservableCollection<T>(collection).WhereSelectLive(predicate, selector);
	}
	/// <summary>
	/// Creates an observable collection whose content reflects the content of the specified collection, filtered by the specified predicate and mapped by the specified selector.
	/// This collection is kept up-to-date with respect to changes in the original colletion.
	/// </summary>
	public static MyReadOnlyObservableCollection<TResult> WhereSelectLive<T, TResult>(this IReadOnlyObservableCollection<T> collection, Func<T, bool> predicate, Func<T, TResult> selector)
	{
		Contract.Requires(collection != null);
		Contract.Requires(selector != null);

		var result = new ProperObservableCollection<TResult>();
		collection.CollectionChanged += collectionChanged;
		collectionChanged(collection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
		return new MyReadOnlyObservableCollection<TResult>(result);

		void collectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Contract.Requires(sender == collection);

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					result.InsertRange(e.NewStartingIndex, e.NewItems!.Cast<T>().Where(predicate).Select(selector));
					break;
				case NotifyCollectionChangedAction.Remove:
					result.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
					break;
				case NotifyCollectionChangedAction.Replace:
					result.Replace(e.OldStartingIndex, e.OldItems!.Count, e.NewItems!.Cast<T>().Where(predicate).Select(selector));
					break;
				case NotifyCollectionChangedAction.Move:
					result.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
					break;
				case NotifyCollectionChangedAction.Reset:
					result.Clear();
					break;
				default:
					throw new ArgumentException("No defined NotifyCollectionChangedEventArgs.Action specified", nameof(e));
			}
		}
	}
	/// <summary>
	/// Creates an observable collection whose content reflects the content of the specified collection, filtered by the specified predicate and mapped by the specified many selector.
	/// This collection is kept up-to-date with respect to changes in the original colletion.
	/// </summary>
	public static MyReadOnlyObservableCollection<TResult> WhereSelectManyLive<T, TResult>(this ObservableCollection<T> collection, Func<T, bool> predicate, Func<T, IEnumerable<TResult>> selector)
	{
		Contract.Requires(collection != null);
		Contract.Requires(selector != null);

		List<int> elementCountsPerSourceElement = new List<int>();
		int cumulativeElements(int sourceElementIndex)
		{
			return elementCountsPerSourceElement.Take(sourceElementIndex).Sum();
		}

		var result = new ProperObservableCollection<TResult>();
		collection.CollectionChanged += collectionChanged;
		collectionChanged(collection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
		return new MyReadOnlyObservableCollection<TResult>(result);

		void collectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Contract.Requires(sender == collection);

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				{
					int sourceItemIndex = e.NewStartingIndex;
					int resultIndex = cumulativeElements(e.NewStartingIndex);
					foreach (var newSourceItem in e.NewItems!.Cast<T>().Where(predicate))
					{
						int originalResultCount = result.Count;
						result.InsertRange(resultIndex, selector(newSourceItem));
						int selectedElementCount = result.Count - originalResultCount;

						resultIndex += selectedElementCount;
						elementCountsPerSourceElement.Insert(sourceItemIndex++, selectedElementCount);
					}
					break;
				}
				case NotifyCollectionChangedAction.Remove:
				{
					int resultIndex = cumulativeElements(e.OldStartingIndex);
					int resultToRemoveCount = elementCountsPerSourceElement.Skip(e.OldStartingIndex).Take(e.OldItems!.Count).Sum();
					result.RemoveRange(resultIndex, resultToRemoveCount);
					elementCountsPerSourceElement.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					break;
				}
				case NotifyCollectionChangedAction.Replace:
					result.Replace(e.OldStartingIndex, e.OldItems!.Count, e.NewItems!.Cast<T>().Where(predicate).SelectMany(selector));
					break;
				case NotifyCollectionChangedAction.Move:
					result.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
					break;
				case NotifyCollectionChangedAction.Reset:
					result.Clear();
					break;
				default:
					throw new ArgumentException("No defined NotifyCollectionChangedEventArgs.Action specified", nameof(e));
			}
		}
	}

	//TODO: make the above readonly collections
	/// <summary>
	/// Syncs the contents of two observable collections by two selectors.
	/// </summary>
	public static ProperObservableCollection<TResult> SyncSelect<T, TResult>(this ProperObservableCollection<T> collection, Func<T, TResult> selector, Func<TResult, T> reverseSelector)
	{
		var result = new ProperObservableCollection<TResult>();
		Sync<T, TResult>(collection, result, selector, reverseSelector);
		return result;
	}
	/// <summary>
	/// Syncs the contents of two observable collections by two selectors.
	/// </summary>
	public static void Sync<T, TResult>(this ProperObservableCollection<T> collection,
										ProperObservableCollection<TResult> secondCollection,
										Func<T, TResult> selector,
										Func<TResult, T> reverseSelector)
	{
		Contract.Requires(collection != null);
		Contract.Requires(selector != null);
		Contract.Requires(reverseSelector != null);

		bool preventBackSync = false;
		collection.CollectionChanged += collectionChanged;
		if (collection.Count != 0)
			collectionChanged(collection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
		secondCollection.CollectionChanged += reverseCollectionChanged;

		//returns whether the sync operation should be performed
		bool toggleSyncFlag()
		{
			preventBackSync = !preventBackSync;
			return preventBackSync;
		}
		void collectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (toggleSyncFlag())
			{
				collectionChanged<T, TResult>(e, secondCollection, selector);
			}
		}
		void reverseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (toggleSyncFlag())
			{
				collectionChanged<TResult, T>(e, collection, reverseSelector);
			}
		}
	}

	private static void collectionChanged<T, TResult>(NotifyCollectionChangedEventArgs e, ProperObservableCollection<TResult> result, Func<T, TResult> selector)
	{
		Contract.Requires(e != null);
		Contract.Requires(result != null);
		Contract.Requires(selector != null);

		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				if (e.NewStartingIndex == -1)
					result.AddRange(e.NewItems!.Cast<T>().Select(selector));
				else
					result.InsertRange(e.NewStartingIndex, e.NewItems!.Cast<T>().Select(selector));
				break;
			case NotifyCollectionChangedAction.Remove:
				result.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
				break;
			case NotifyCollectionChangedAction.Replace:
				result.Replace(e.OldStartingIndex, e.OldItems!.Count, e.NewItems!.Cast<T>().Select(selector));
				break;
			case NotifyCollectionChangedAction.Move:
				result.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
				break;
			case NotifyCollectionChangedAction.Reset:
				result.Clear();
				break;
			default:
				throw new ArgumentException("No defined NotifyCollectionChangedEventArgs.Action specified", nameof(e));
		}
	}

}
