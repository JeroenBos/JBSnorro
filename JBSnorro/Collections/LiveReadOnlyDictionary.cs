using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections;

/// <summary> A readonly wrapper around a dictionary, mapping its keys and/or values live. </summary>
/// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
public sealed class LiveReadOnlyDictionary<TKey, TValue, T, U> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly IReadOnlyDictionary<T, U> data;
    private readonly Func<TKey, T> getKey;
    private readonly Func<U, TValue> getValue;

    public LiveReadOnlyDictionary(IReadOnlyDictionary<T, U> dictionary, Func<TKey, T> getKey, Func<U, TValue> getValue)
    {
        this.data = dictionary;
        this.getKey = getKey;
        this.getValue = getValue;
    }

    public TValue this[TKey key] => getValue(this.data[getKey(key)]);

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => throw KeysNotStoredException();

    public IEnumerable<TValue> Values => this.data.Values.Select(getValue);

    public int Count => this.data.Count;

    public bool ContainsKey(TKey key)
    {
        return this.data.ContainsKey(getKey(key));
    }
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var result = this.data.TryGetValue(getKey(key), out var intermediateValue);
        if (result)
        {
            value = getValue(intermediateValue!);
        }
        else
        {
            value = default;
        }
        return result;
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        throw KeysNotStoredException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw KeysNotStoredException();
    }
    [DebuggerHidden]
    private static Exception KeysNotStoredException()
    {
        return new NotSupportedException("Because the keys aren't stored");
    }
}
