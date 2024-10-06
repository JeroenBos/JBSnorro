using System.Collections;
using System.Diagnostics;

namespace JBSnorro.Collections;

/// <summary>
/// Represents a list where each item on the list is checked for a predicate, so —assuming it doesn't mutate and violate the predicate— all items on this list satisfy the predicate.
/// </summary>
public class PredicatedList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> data;
    private readonly Func<T, string?> predicate;

    [DebuggerHidden]
    public PredicatedList(Func<T, bool> predicate)
        : this([DebuggerHidden] (item) => predicate(item) ? "" : null)
    {
    }
    [DebuggerHidden]
    public PredicatedList(Func<T, bool> predicate, int capacity)
        : this([DebuggerHidden] (item) => predicate(item) ? "" : null, capacity)
    {
    }
    [DebuggerHidden]
    public PredicatedList(Func<T, bool> predicate, IEnumerable<T> initialCollection)
        : this([DebuggerHidden] (item) => predicate(item) ? "" : null, initialCollection)
    { }

    [DebuggerHidden]
    public PredicatedList(Func<T, string?> predicate)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        this.data = new List<T>();
    }
    [DebuggerHidden]
    public PredicatedList(Func<T, string?> predicate, int capacity)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        this.data = new List<T>(capacity);
    }
    [DebuggerHidden]
    public PredicatedList(Func<T, string?> predicate, IEnumerable<T> initialCollection)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        this.data = initialCollection.TryGetNonEnumeratedCount(out int count) ? new List<T>(count + 4) : new List<T>();
        this.AddRange(initialCollection ?? throw new ArgumentNullException(nameof(initialCollection)));
    }

    /// <param name="index">Only for when adding a range.</param>
    [DebuggerHidden]
    private void ValidatePredicate(T item, int? index = null)
    {
        string? error = predicate(item);
        if (error is not null)
        {
            string message = index is null ? "Specified item does not match the collection's predicate" : $"Specified item (index={index}) does not match the collection's predicate";
            if (!string.IsNullOrWhiteSpace(error))
            {
                message += ". Details:\n" + error;
            }
            throw new ArgumentNullException(nameof(item), message);
        }
    }
    public T this[int index]
    {
        [DebuggerHidden]
        get => this.data[index];
        [DebuggerHidden]
        set
        {
            this.ValidatePredicate(value);
            this.data[index] = value;
        }
    }
    [DebuggerHidden]
    public int Count => this.data.Count;
    [DebuggerHidden]
    public void Add(T item)
    {
        this.ValidatePredicate(item);
        this.data.Add(item);
    }
    [DebuggerHidden]
    public void AddRange(IEnumerable<T> items)
    {
        int i = 0;
        foreach (var item in items ?? throw new ArgumentNullException(nameof(items)))
        {
            this.ValidatePredicate(item, i);
            this.data.Add(item);
            i++;
        }
    }
    [DebuggerHidden]
    public void Clear()
    {
        this.data.Clear();
    }
    [DebuggerHidden]
    public bool Contains(T item)
    {
        return this.data.Contains(item);
    }
    [DebuggerHidden]
    public void CopyTo(T[] array, int arrayIndex)
    {
        this.data.CopyTo(array, arrayIndex);
    }
    [DebuggerHidden]
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)this.data).GetEnumerator();
    }
    [DebuggerHidden]
    public int IndexOf(T item)
    {
        return this.data.IndexOf(item);
    }
    [DebuggerHidden]
    public void Insert(int index, T item)
    {
        this.ValidatePredicate(item);
        this.data.Insert(index, item);
    }
    [DebuggerHidden]
    public bool Remove(T item)
    {
        return this.data.Remove(item);
    }
    [DebuggerHidden]
    public void RemoveAt(int index)
    {
        this.data.RemoveAt(index);
    }
    [DebuggerHidden]
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)this.data).IsReadOnly;
    [DebuggerHidden]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)this.data).GetEnumerator();
    }
}
