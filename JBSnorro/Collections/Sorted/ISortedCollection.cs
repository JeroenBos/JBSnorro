namespace JBSnorro.Collections.Sorted;

/// <summary> Represents a sorted indexable collection with known count. </summary>
public interface ISortedList<T> : IIndexable<T>, ICountable<T>, ISortedEnumerable<T>
{
}
