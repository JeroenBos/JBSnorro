#nullable enable
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections;

public class Cache<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    /// <param name="capacity">The maximum number of items in the cache. 0 means limitless.</param>
    public static Cache<TKey, TValue> Create(Func<TKey, TValue> f, int capacity = 0, IEqualityComparer<TKey>? equalityComparer = null)
    {
        Contract.Requires(capacity >= 0);
        var cache = capacity == 0 ? new Dictionary<TKey, TValue>(equalityComparer) : new Dictionary<TKey, TValue>(capacity: capacity, equalityComparer);
        return new(f, cache, capacity, equalityComparer);
    }
    /// <param name="capacity">The maximum number of items in the cache. 0 means limitless.</param>
    public static Cache<TKey, TValue> CreateThreadSafe(Func<TKey, TValue> f, int capacity = 0, IEqualityComparer<TKey>? equalityComparer = null)
    {
        return new(f, new ConcurrentDictionary<TKey, TValue>(equalityComparer), capacity, equalityComparer);
    }
    private readonly Func<TKey, TValue> f;
    private readonly IDictionary<TKey, TValue> cache;
    private readonly PriorityQueue<TKey>? queue;
    public int Capacity { get; }

    public TValue this[TKey key]
    {
        get
        {
            if (this.cache.TryGetValue(key, out TValue? result))
            {
                queueTouch(key);
            }
            else
            {
                result = f(key);
                queueAdd(key);
                cache[key] = result;
            }

            return result;
        }
    }

    private Cache(Func<TKey, TValue> f, IDictionary<TKey, TValue> cache, int capacity, IEqualityComparer<TKey>? equalityComparer)
    {
        this.f = f;
        this.cache = cache;
        this.Capacity = capacity;
        if (capacity > 0)
        {
            this.queue = new PriorityQueue<TKey>(equalityComparer);
        }
    }

    private void queueTouch(TKey key)
    {
        if (this.queue == null)
        {
            return;
        }
        this.queue.Touch(key);
    }
    private void queueAdd(TKey key)
    {
        if (this.queue == null)
        {
            return;
        }
        if (this.queue.Count >= this.Capacity)
        {
            cache.Remove(this.queue.Pop());
        }
        this.queue.Add(key);
    }

    /// <summary>
    /// Gets whether the specified key has a cached value already.
    /// </summary>

    [DebuggerHidden]
    public bool ContainsKey(TKey key)
    {
        return cache.ContainsKey(key);
    }
    /// <summary>
    /// Tries to get the value from the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [DebuggerHidden]
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.cache.TryGetValue(key, out value))
        {
            queueTouch(key);
            return true;
        }
        return false;
    }
    [DebuggerHidden] int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => this.cache.Count;
    [DebuggerHidden] IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.cache.Keys;
    [DebuggerHidden] IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.cache.Values;
    [DebuggerHidden] IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => this.cache.GetEnumerator();
    [DebuggerHidden] IEnumerator IEnumerable.GetEnumerator() => this.cache.GetEnumerator();
}
