using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;

namespace JBSnorro.Collections.Sorted;

public interface ISortedReadOnlyList<T> : IReadOnlyList<T>, ISortedEnumerable<T>
{
}
public class SortedReadOnlyList<T> : ISortedReadOnlyList<T>
{
	//exactly one of data1 and data2 is null.
	//type hierarchy in BCL doesn't contain a type that's enumerable, countable and indexable....

	private readonly IReadOnlyList<T>? data3;
	private readonly ISortedList<T>? data2;
	private readonly IList<T>? data1;
	private readonly Func<T, T, int> comparer;

	public SortedReadOnlyList(List<T> sortedData, Func<T, T, int>? comparer = null) : this((IList<T>)sortedData, comparer) { }
	public SortedReadOnlyList(IReadOnlyList<T> sortedData, Func<T, T, int>? comparer = null)
	{
		Contract.Requires(sortedData != null);

		comparer = comparer.OrDefault();

		Contract.Requires(sortedData.IsSorted(comparer));
		this.data3 = sortedData;
		this.comparer = comparer;
	}
	public SortedReadOnlyList(IList<T> sortedData, Func<T, T, int>? comparer = null)
	{
		Contract.Requires(sortedData != null);

		comparer = comparer.OrDefault();

		Contract.Requires(sortedData.IsSorted(comparer));
		this.data1 = sortedData;
		this.comparer = comparer;
	}

	public SortedReadOnlyList(ISortedList<T> sortedData)
	{
		Contract.Requires(sortedData != null);
		Contract.Requires(sortedData.Comparer != null);

		this.data2 = sortedData;
		this.comparer = sortedData.Comparer;
	}

	[DebuggerHidden]
	private bool IsSorted()
	{
		if (data1 != null)
			return data1.IsSorted(comparer);
		else if (data2 != null)
			return data2.IsSorted(comparer);
		else
			return data3!.IsSorted(comparer);
	}

	public int Count
	{
		get { return data1?.Count ?? data2?.Count ?? data3!.Count; }
	}

	public Func<T, T, int> Comparer
	{
		get { return comparer; }
	}

	public T this[int index]
	{
		get
		{
			Contract.Requires(this.IsSorted());
			Contract.Requires<IndexOutOfRangeException>(0 <= index && index < this.Count);

			if (data1 != null)
				return data1[index];
			else if (data2 != null)
				return data2[index];
			else
				return data3![index];
		}
	}

	[DebuggerHidden]
	public IEnumerator<T> GetEnumerator()
	{
		Contract.Requires(this.IsSorted());//cannot use extension method, for then an infinite loop would reign
		return (data1 ?? (IEnumerable<T>?)data2 ?? data3!).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public static SortedReadOnlyList<T> Create(Func<T, T, int> comparer)
	{
		return new SortedReadOnlyList<T>((IList<T>)EmptyCollection<T>.Array, comparer);
	}
	public static SortedReadOnlyList<T> Create(IComparer<T> comparer)
	{
		return Create(comparer.Compare);
	}
	public static SortedReadOnlyList<T> Create(Func<T, T, int> comparer, T singletonElement)
	{
		return new SortedReadOnlyList<T>((IList<T>)new[] { singletonElement }, comparer);
	}
	public static SortedReadOnlyList<T> Create(IComparer<T> comparer, T singletonElement)
	{
		return Create(comparer.Compare, singletonElement);
	}

}
