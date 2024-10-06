using System.Collections;

namespace JBSnorro.Collections;

/// <summary>
/// Represents a list where each item on the list is checked for a predicate, so —assuming it doesn't mutate and violate the predicate— all items on this list satisfy the predicate.
/// </summary>
public class PredicatedList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> data;
    private readonly Func<T, bool> predicate;

    public PredicatedList(Func<T, bool> predicate)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        this.data = new List<T>();
    }
    public PredicatedList(Func<T, bool> predicate, int capacity)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        this.data = new List<T>(capacity);
    }
    public PredicatedList(Func<T, bool> predicate, IEnumerable<T> initialCollection)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        this.data = initialCollection.TryGetNonEnumeratedCount(out int count) ? new List<T>(count + 4) : new List<T>();
        foreach (var item in initialCollection ?? throw new ArgumentNullException(nameof(initialCollection)))
        {
            this.Add(item); // validates
        }
    }

    private void ValidatePredicate(T item)
    {
        if (!predicate(item))
        {
            throw new ArgumentNullException(nameof(item), "Specified item does not match the collection's predicate");
        }
    }
    public T this[int index]
    {
        get => this.data[index];
        set
        {
            this.ValidatePredicate(value);
            this.data[index] = value;
        }
    }
    public int Count => this.data.Count;
    public void Add(T item)
    {
        this.ValidatePredicate(item);
        this.data.Add(item);
    }
    public void Clear()
    {
        this.data.Clear();
    }
    public bool Contains(T item)
    {
        return this.data.Contains(item);
    }
    public void CopyTo(T[] array, int arrayIndex)
    {
        this.data.CopyTo(array, arrayIndex);
    }
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)this.data).GetEnumerator();
    }
    public int IndexOf(T item)
    {
        return this.data.IndexOf(item);
    }
    public void Insert(int index, T item)
    {
        this.ValidatePredicate(item);
        this.data.Insert(index, item);
    }
    public bool Remove(T item)
    {
        return this.data.Remove(item);
    }
    public void RemoveAt(int index)
    {
        this.data.RemoveAt(index);
    }
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)this.data).IsReadOnly;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)this.data).GetEnumerator();
    }
}
