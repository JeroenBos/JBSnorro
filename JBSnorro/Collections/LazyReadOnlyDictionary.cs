using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections;

public class LazyReadOnlyDictionary<TSource, TValue> : IReadOnlyDictionary<TSource, TValue>
{
    private readonly IReadOnlyList<TValue> collection;
    private readonly Func<TSource, int> keySelector;

    public LazyReadOnlyDictionary(IReadOnlyList<TValue> collection, Func<TSource, int> keySelector)
    {
        this.collection = collection;
        this.keySelector = keySelector;
    }

    public TValue this[TSource source]
    {
        get
        {
            if (this.TryGetValue(source, out var result))
            {
                return result;
            }
            throw new KeyNotFoundException();
        }
    }
    public int Count => collection.Count();

    IEnumerable<TSource> IReadOnlyDictionary<TSource, TValue>.Keys => throw new NotSupportedException();
    public IEnumerable<TValue> Values => collection;


    public bool ContainsKey(TSource source)
    {
        return keySelector(source) != -1;
    }
    public bool TryGetValue(TSource source, [MaybeNullWhen(false)] out TValue value)
    {
        int key = keySelector(source);
        if (key == -1)
        {
            value = default;
            return false;
        }
        value = collection[key];
        return true;
    }

    IEnumerator<KeyValuePair<TSource, TValue>> IEnumerable<KeyValuePair<TSource, TValue>>.GetEnumerator() => throw new NotSupportedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
}
