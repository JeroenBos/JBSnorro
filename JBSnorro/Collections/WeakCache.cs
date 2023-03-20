#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.SystemTypes;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections;

public static class WeaklyCachedFactory
{
    public static WeaklyCachedFactory<TKey, TValue> Create<TKey, TValue>(Func<TKey, TValue> factory, IEqualityComparer<TKey>? keyEqualityComparer = null)
        where TKey : notnull where TValue : class
    {
        return new WeaklyCachedFactory<TKey, TValue>(factory, keyEqualityComparer);
    }
    public static WeaklyCachedFactoryWithNullKey<TKey, TValue> CreateWithNullableKey<TKey, TValue>(Func<TKey?, TValue> factory, IEqualityComparer<TKey>? keyEqualityComparer = null)
        where TKey : class where TValue : class
    {
        return new WeaklyCachedFactoryWithNullKey<TKey, TValue>(factory, keyEqualityComparer);
    }
}
/// <summary>
/// Represents a cache of a factory method, where the constructed values are held weakly.
/// </summary>
public class WeaklyCachedFactory<TKey, TValue> : WeakCacheBase<TKey, TValue> where TKey : notnull where TValue : class
{
    protected readonly Func<TKey, TValue> factory;

    [DebuggerHidden]
    public WeaklyCachedFactory(Func<TKey, TValue> factory, IEqualityComparer<TKey>? keyEqualityComparer = null)
        : base(keyEqualityComparer)
    {
        this.factory = factory;
    }

    [DebuggerHidden]
    public void Add(TKey key)
    {
        base.Add(key, factory(key));
    }
}
/// <summary>
/// Represents a cache of a factory method, where the constructed values are held weakly. 
/// Dictionaries typically don't allow a null key, but this class purposefully does.
/// </summary>
public sealed class WeaklyCachedFactoryWithNullKey<TKey, TValue> : WeaklyCachedFactory<TKey, TValue> where TKey : class where TValue : class
{
    private Option<TValue> nullValue;

    [DebuggerHidden]
    public WeaklyCachedFactoryWithNullKey(Func<TKey?, TValue> factory, IEqualityComparer<TKey>? keyEqualityComparer = null)
        : base(factory, keyEqualityComparer)
    {
    }

    public override TValue this[TKey? key]
    {
        get => ReferenceEquals(null, key) ? this.NullValue : base[key];
    }
    public TValue NullValue
    {
        get
        {
            if (!this.nullValue.HasValue)
                this.nullValue = base.factory(null!);
            return this.nullValue.Value;
        } 
    }
    [DebuggerHidden]
    public new void Add(TKey? key)
    {
        // this method just changes a nullable annotation (so 'new' suffices, as opposed to 'override')
        base.Add(key!, factory(key!));
    }
    public override bool ContainsKey(TKey key)
    {
        if (ReferenceEquals(key, null))
            return nullValue.HasValue;
        return base.ContainsKey(key);
    }
}

/// <summary>
/// This class maps keys to objects, where the objects are references that are held weakly. 
/// </summary>
public class WeakCache<TKey, TValue> : WeakCacheBase<TKey, TValue>, IDictionary<TKey, TValue> where TKey : notnull where TValue : class
{
    // this only implements the mutating members
    [DebuggerHidden] void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, value);
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    int ICollection<KeyValuePair<TKey, TValue>>.Count => base.Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        if (this.TryGetValue(item.Key, out var value))
        {
            return EqualityComparer<TValue>.Default.Equals(item.Value, value);
        }
        return false;
    }
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => base.Remove(item);
    bool IDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);
    bool IDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out value);
    TValue IDictionary<TKey, TValue>.this[TKey key] { get => this[key]; set => this[key] = value; }
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotImplementedException();
    ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotImplementedException();
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();

}
/// <summary>
/// This class is the shared code between <see cref="WeakCache{TKey, TValue}"/> and <see cref="WeaklyCachedFactory{TKey, TValue}"/>.
/// </summary>
public class WeakCacheBase<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull where TValue : class
{
    private readonly Dictionary<TKey, WeakReference<TValue>> data;
    protected int Count => this.data.Count;

    [DebuggerHidden]
    public WeakCacheBase(IEqualityComparer<TKey>? keyEqualityComparer = null)
    {
        data = new Dictionary<TKey, WeakReference<TValue>>(keyEqualityComparer);
    }

    public virtual TValue this[TKey key]
    {
        [DebuggerHidden]
        get
        {
            if (this.TryGetValue(key, out var result))
            {
                return result;
            }
            else
            {
                throw new KeyNotFoundException("The object with the specified key does not exist (anymore)");
            }
        }
        [DebuggerHidden]
        protected set
        {
            data[key] = new WeakReference<TValue>(value);
        }
    }

    /// <summary>
    /// Removes all entries whose weak reference has been garbage collected, thereby possibly freeing up the last reference to the value associated to that reference.
    /// </summary>
    public void Clean()
    {
        var valuesToRemove = this.data
                                 .Where(pair => pair.Value.TryGetTarget(out _))
                                 .Select(pair => pair.Key)
                                 .ToList();

        foreach (var valueToRemove in valuesToRemove)
            this.data.Remove(valueToRemove);
    }
    public void Clear()
    {
        this.data.Clear();
    }
    public virtual bool ContainsKey(TKey key)
    {
        return this.data.ContainsKey(key);
    }
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var pair in this.data)
        {
            if (pair.Value.TryGetTarget(out TValue? target))
            {
                yield return KeyValuePair.Create(pair.Key, target);
            }
        }
    }
    protected bool Remove(TKey key)
    {
        return this.data.Remove(key);
    }
    protected bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (this.TryGetValue(item.Key, out var value)) // cleans
        {
            if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
            {
                bool result = this.data.Remove(item.Key);
                Contract.Assert(result);
                return result;
            }
        }
        return false;
    }
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)][NotNullWhen(true)] out TValue value)
    {
        if (this.data.TryGetValue(key, out var reference))
        {
            if (reference.TryGetTarget(out value))
                return true;
            else
            {
                // clean up
                this.Remove(key);
                Contract.Assert(this.data.TryGetValue(key, out var _) is false, "Value not removed!?");
            }
        }
        value = default;
        return false;
    }

    [DebuggerHidden]
    protected void Add(TKey key, TValue value)
    {
        this.data.Add(key, new WeakReference<TValue>(value));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    // not implemented members
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => throw new NotImplementedException();
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => throw new NotImplementedException();
    /// <summary> This is only approximate (but an upper limit). </summary>
    int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => this.Count;

}
