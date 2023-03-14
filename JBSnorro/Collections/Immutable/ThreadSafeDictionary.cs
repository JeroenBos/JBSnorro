using JBSnorro.Diagnostics;
using System.Collections.Immutable;

namespace JBSnorro.Collections.Immutable;

/// <summary>
/// Represents a dictionary that can mutate only by atomic operations.
/// </summary>
public class ThreadSafeDictionary<TKey, TValue>
{
    private ImmutableDictionary<TKey, TValue> data = ImmutableDictionary.Create<TKey, TValue>();
    private ImmutableDictionary<TKey, TValue> update(Func<ImmutableDictionary<TKey, TValue>> value)
    {
        ImmutableDictionary<TKey, TValue> oldData, setData;
        do
        {
            var newData = value();
            Contract.Assert(newData is not null, $"'{nameof(value)}' may not return null. ");

            oldData = this.data;
            setData = Interlocked.Exchange(ref this.data, newData);
        }
        while (oldData != setData);
        return oldData;
    }

    public void Add(TKey key, TValue value)
    {
        ImmutableInterlocked.AddOrUpdate(ref data, key, value, (key, oldValue) => { throw new Exception("key already present"); });
    }

    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        this.update(() => this.data.AddRange(items));
    }

    public int Count => this.data.Count;
}
