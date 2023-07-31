using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;

namespace JBSnorro;

public class SortedEnumerable<T> : ISortedEnumerable<T>
{
	private readonly IEnumerable<T> sequence;
	[DebuggerHidden]
	public SortedEnumerable(IEnumerable<T> sequence, IComparer<T> comparer) : this(sequence, comparer.Compare) { }
	[DebuggerHidden]
	public SortedEnumerable(IEnumerable<T> sequence, Func<T, T, int>? comparer = null)
	{
		comparer = comparer.OrDefault();

		Contract.Requires(sequence != null);
		Contract.LazilyAssertSortedness(ref sequence, comparer);

		this.sequence = sequence;
		this.Comparer = comparer;
	}
	public SortedEnumerable(IEnumerable<T> sequence, Func<T, IComparable> comparableKeySelector)
		: this(sequence, (a, b) => comparableKeySelector(a).CompareTo(comparableKeySelector(b)))
	{
	}
	[DebuggerHidden]
	public IEnumerator<T> GetEnumerator()
	{
		return sequence.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	public Func<T, T, int> Comparer { get; private set; }


}
public static class SortedEnumerable
{
	public static SortedEnumerable<int> Range(int start, int count)
	{
		return new SortedEnumerable<int>(Enumerable.Range(start, count));
	}

}
