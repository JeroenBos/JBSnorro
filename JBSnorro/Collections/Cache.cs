#nullable enable
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
using JBSnorro.Extensions;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections;

public class Cache<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    public static Cache<TKey, TValue> Create(Func<TKey, TValue> f, IEqualityComparer<TKey>? equalityComparer = null)
    {
        return new(f, new Dictionary<TKey, TValue>(equalityComparer));
    }
    public static Cache<TKey, TValue> CreateThreadSafe(Func<TKey, TValue> f, IEqualityComparer<TKey>? equalityComparer = null)
    {
        return new(f, new ConcurrentDictionary<TKey, TValue>(equalityComparer));
    }
    private readonly Func<TKey, TValue> f;
    private readonly IDictionary<TKey, TValue> cache;

    public TValue this[TKey key]
    {
        get => cache.GetOrAdd(key, f);
    }

    private Cache(Func<TKey, TValue> f, IDictionary<TKey, TValue> cache)
    {
        this.f = f;
        this.cache = cache;
    }


    /// <summary>
    /// Gets whether the specified key has a cached value already.
    /// </summary>

    [DebuggerHidden] public bool ContainsKey(TKey key)
    {
        return cache.ContainsKey(key);
    }
    /// <summary>
    /// Tries to get the value from the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [DebuggerHidden] public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => this.cache.TryGetValue(key, out value);
    [DebuggerHidden] int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => this.cache.Count;
    [DebuggerHidden] IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.cache.Keys;
    [DebuggerHidden] IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.cache.Values;
    [DebuggerHidden] IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => this.cache.GetEnumerator();
    [DebuggerHidden] IEnumerator IEnumerable.GetEnumerator() => this.cache.GetEnumerator();
}
