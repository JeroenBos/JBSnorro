using System.Collections.Immutable;

namespace JBSnorro.Collections.Immutable;

/// <summary>
/// Represents a list that can mutate only by atomic operations.
/// </summary>
public class ThreadSafeList<T> where T : class
{
    private ImmutableList<T> _data = ImmutableList.Create<T>();
    private ImmutableList<T> update(Func<ImmutableList<T>, ImmutableList<T>> value)
    {
        var priorCollection = this._data;
        ImmutableList<T> newData, interlockedResult;
        do
        {
            newData = value(priorCollection);
            if (newData == null) { throw new ArgumentException($"'{nameof(value)}' may not return null"); }

            interlockedResult = Interlocked.CompareExchange(ref this._data, newData, priorCollection);
        }
        while (!ReferenceEquals(priorCollection, interlockedResult));
        return newData;
    }
    /// <summary>
    /// Clears the current list and returns the values it had at that moment.
    /// </summary>
    public IReadOnlyList<T> Clear()
    {
        return update(data => ImmutableList.Create<T>());
    }
    public void Add(T item)
    {
        this.update(data => data.Add(item));
    }

    public void AddRange(IEnumerable<T> items)
    {
        this.update(data => data.AddRange(items));
    }

    public int Count => this._data.Count;
}
