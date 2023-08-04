using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;

namespace JBSnorro;

public interface ITinyIndex
{
	int Index { get; }
}

/// <summary> Holds extensions methods a la EnumerableExtensions for the ITinyIndex </summary>
public static class ITinyIndexExtensions
{
	/// <summary> Gets whether the specified sequence is (non-strictly) increasing. </summary>
	public static bool AreIncreasing(this IEnumerable<ITinyIndex> indices)
	{
		return indices.Select(i => i.Index).AreIncreasing();
	}
	/// <summary> Gets whether the specified sequence is sequential. </summary>
	public static bool AreSequential(this IEnumerable<ITinyIndex> indices)
	{
		return indices.AreSequential((i, j) => i.Index + 1 == j.Index);
	}
	[DebuggerHidden]
	public static IEnumerable<int> ToInts(this IEnumerable<ITinyIndex> indices)
	{
		return indices.Select(i => i.Index);
	}
}
public interface ITinyIndexedCollection<T, in TIndex> : ICollection<T> where TIndex : ITinyIndex
{
	T this[TIndex index] { get; set; }
	void Insert(TIndex index, T item);
}

public interface IReadOnlyTinyIndexedCollection<out T, in TIndex> : IReadOnlyCollection<T> where TIndex : ITinyIndex
{
	T this[TIndex index] { get; }
}

public class TinyIndexedCollection<T, TIndex> : ITinyIndexedCollection<T, TIndex> where TIndex : ITinyIndex
{
	private readonly List<T> data;

	public TinyIndexedCollection(IEnumerable<T>? initialData = null, int initialCapacity = 4)
	{
		Contract.Requires(initialCapacity >= 0);

		data = new List<T>(initialCapacity);
		if (initialData != null)
			data.AddRange(initialData);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return data.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	public void Add(T item)
	{
		data.Add(item);
	}
	public void Clear()
	{
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
	public bool Remove(T item)
	{
		return data.Remove(item);
	}
	public int Count
	{
		get { return data.Count; }
	}
	bool ICollection<T>.IsReadOnly
	{
		get { return false; }
	}
	public T this[TIndex index]
	{
		[DebuggerHidden]
		get { return data[index.Index]; }
		[DebuggerHidden]
		set { data[index.Index] = value; }
	}
	public void Insert(TIndex index, T item)
	{
		data.Insert(index.Index, item);
	}
}

public class ReadOnlyTinyIndexedCollection<T, TIndex> : IReadOnlyTinyIndexedCollection<T, TIndex> where TIndex : ITinyIndex
{
	public IReadOnlyList<T> Collection { get; private set; }
	public ReadOnlyTinyIndexedCollection(IReadOnlyList<T> collection)
	{
		Contract.Requires(collection != null);

		this.Collection = collection;
	}
	/*public ReadOnlyTinyIndexedCollection(IList<T> collection)
	{
		Contract.Requires(collection != null);

		this.Collection = collection;
	}*/

	public T this[TIndex index]
	{
		get
		{
			Contract.Requires<ArgumentOutOfRangeException>(0 <= index.Index && index.Index < Count);
			return Collection[index.Index];
		}
	}
	public int Count
	{
		get
		{
			return Collection.Count;
		}
	}
	[DebuggerHidden]
	public IEnumerator<T> GetEnumerator()
	{
		return Collection.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public DeferredReadOnlyTinyIndexedCollection<TResult, T, TIndex> Select<TResult>(Func<T, TResult> map)
	{
		Contract.Requires(map != null);
		return new DeferredReadOnlyTinyIndexedCollection<TResult, T, TIndex>(this, map);
	}


}

public class DeferredReadOnlyTinyIndexedCollection<T, TSource, TIndex> : IReadOnlyTinyIndexedCollection<T, TIndex> where TIndex : ITinyIndex
{
	private readonly IReadOnlyTinyIndexedCollection<TSource, TIndex> source;
	private readonly Func<TSource, T> map;
	public DeferredReadOnlyTinyIndexedCollection(IReadOnlyTinyIndexedCollection<TSource, TIndex> source, Func<TSource, T> map)
	{
		this.source = source;
		this.map = map;
	}
	public T this[TIndex index]
	{
		get { return map(source[index]); }
	}
	public int Count
	{
		get { return source.Count; }
	}
	[DebuggerHidden]
	public IEnumerator<T> GetEnumerator()
	{
		return source.Select(element => map(element)).GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
