using JBSnorro.Diagnostics;
using System.Collections;
using System.Diagnostics;

namespace JBSnorro.Collections;

[DebuggerDisplay("Count = {Count}")]
public sealed class Heap<T> : ICollection<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Func<T, T, int> comparer;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly List<T> data;
    /// <summary> Gets the ith element on this heap, with i = 0 being the smallest T, but for other i this won't return elements in sorted order. </summary>
    /// <param name="i"> The index of the element to get. </param>
    public T this[int i]
    {
        get
        {
            Contract.Requires(i >= 0);
            Contract.Requires(i < this.Count);
            return data[i];
        }
        set
        {
            Contract.Requires(i >= 0);
            Contract.Requires(i < this.Count);
            Contract.Requires(value != null);

            int comparisonResult = comparer(data[i], value);
            data[i] = value;
            if (comparisonResult < 0)
                bubbleDown(i);
            else if (comparisonResult > 0)
                bubbleUp(i);
        }
    }
    /// <summary> Gets the number of elements on this heap. </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public int Count
    {
        get
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            return data.Count;
        }
    }
    /// <summary> Gets whether this heap does not contain members. </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsEmpty
    {
        get { return this.Count == 0; }
    }

    /// <summary> Creates a new heap without elements and the default capacity. </summary>
    /// <param name="comparer"> The comparer between two instances of type T. To select IComparable&lt;T&gt;.CompareTo(T), leave it to null. </param>
    public Heap(Func<T, T, int>? comparer = null)
    {
        this.comparer = comparer ?? Comparer<T>.Default.Compare;
        data = new List<T>();
    }
    /// <summary> Creates a new heap. </summary>
    /// <param name="capacity"> The capacity of the new heap. </param>
    /// <param name="comparer"> The comparer between two instances of type T. To select IComparable&lt;T&gt;.CompareTo(T), leave it to null. </param>
    public Heap(int capacity, Func<T, T, int>? comparer = null)
        : this(comparer)
    {
        Contract.Requires(capacity >= 0);

        data = new List<T>(capacity);
    }
    /// <summary> Creates a new heap. </summary>
    /// <param name="initialElements"> The initial elements in this heap. </param>
    /// <param name="comparer"> The comparer between two instances of type T. To select IComparable&lt;T&gt;.CompareTo(T), leave it to null. </param>
    public Heap(IEnumerable<T> initialElements, Func<T, T, int>? comparer = null)
        : this(comparer)
    {
        Contract.Requires(initialElements != null);

        data = new List<T>(initialElements);
        data.Sort(new Comparison<T>(this.comparer));
    }


    /// <summary> Removes and returns the lowest element on the heap. Throws if there are no elements. </summary>
    public T RemoveNext()
    {
        Contract.Requires(this.Count != 0);

        var min = data[0];
        data[0] = data[data.Count - 1];
        data.RemoveAt(data.Count - 1);
        bubbleDown(0);
        return min;
    }
    /// <summary> Adds a specified item to the heap, and ensures it is sorted. </summary>
    /// <param name="item"> The item to add to the heap. Cannot be null, since it has to be compared with other items. </param>
    public void Add(T item)
    {
        Contract.Requires(item != null);

        data.Add(item);
        bubbleUp(data.Count - 1);
    }
    /// <summary> Adds a range of items to the heaps and ensures it is sorted. Is not faster than adding one by one. </summary>
    /// <param name="items"></param>
    public void AddRange( IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException("items");

        //TODO: think about optimizing this
        foreach (var item in items)
            this.Add(item);
    }
    /// <summary> Returns whether this heap contains the specified item. </summary>
    /// <param name="item"> The item to search for. </param>
    public bool Contains(T item)
    {
        return this.data.Contains(item);//Can be done using the properties of a Heap<T> but is O(n) too, therefore not worth implementing.
    }
    public void Clear()
    {
        this.data.Clear();
    }
    public void CopyTo(T[] array, int arrayIndex)
    {
        this.data.CopyTo(array, arrayIndex);
    }
    public bool Remove(T item)
    {
        return this.data.Remove(item);
    }
    public bool IsReadOnly => false;

    private void bubbleDown(int index)
    {
        Contract.Requires(index >= 0);
        //Contract.Requires(index < this.Count); not necessary. DownBubble won't crash in that case.

        while (true)
        {
            int child = (index << 1) + 1;
            int c0 = child + 1;
            if (child >= data.Count)
                break;
            if (c0 < data.Count && comparer(data[child], data[c0]) > 0)
            {
                child = c0;
            }
            Contract.Assume(index < data.Count);
            if (comparer(data[index], data[child]) > 0)
            {
                var temp = data[index];
                data[index] = data[child];
                data[child] = temp;
                index = child;
            }
            else
            {
                break;
            }
        }
    }
    private void bubbleUp(int index)
    {
        Contract.Requires(index >= 0);
        Contract.Requires(index < this.Count);

        while (true)
        {
            int parent = (index - 1) >> 1;
            if (index == 0)
                break;
            if (comparer(data[index], data[parent]) < 0)
            {
                var temp = data[index];
                data[index] = data[parent];
                data[parent] = temp;
                index = parent;
            }
            else
                break;
        }
    }


    public IEnumerator<T> GetEnumerator()
    {
        Contract.Ensures(Contract.Result<IEnumerator<T>>() != null);
        return this.data.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        Contract.Ensures(Contract.Result<System.Collections.IEnumerator>() != null);
        return this.GetEnumerator();
    }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private T[] displayList
    {
        get { return this.data.Take(this.Count).ToArray(); }
    }
}
