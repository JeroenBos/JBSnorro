using JBSnorro.Diagnostics;
using System.Collections;

namespace JBSnorro.Collections;

/// <summary>
/// Represents an immutable list consisting of an element any number of times.
/// </summary>
public class ConstantReadOnlyList<T> : IReadOnlyList<T>
{
    public T Value { get; }
    public int Count { get; }

    public ConstantReadOnlyList(T value, int count)
    {
        Contract.Requires(count >= 0);

        this.Value = value;
        this.Count = count;
    }

    public T this[int index]
    {
        get
        {
            Contract.Requires<IndexOutOfRangeException>(index >= 0);
            Contract.Requires<IndexOutOfRangeException>(index < Count);
            return this.Value;
        }
    }
    public IEnumerator<T> GetEnumerator()
    {
        return Enumerable.Range(0, this.Count)
                         .Select(_ => Value)
                         .GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
