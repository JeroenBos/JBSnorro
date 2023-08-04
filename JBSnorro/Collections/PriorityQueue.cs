using JBSnorro.Diagnostics;

namespace JBSnorro.Collections;

/// <summary>
/// A queue that keeps track of a list of items in order that they were last touched.
/// So this is very much like a normal queue, except that it allows for easy re-enqueuing of elements already in the queue.
/// </summary>
public class PriorityQueue<T>
{
    private readonly LinkedList<T> queue;
    private readonly HashSet<LinkedListNode<T>> values;

    public PriorityQueue(IEqualityComparer<T>? equalityComparer = null)
    {
        var mappedEqualityComparer = (equalityComparer ?? EqualityComparer<T>.Default).Map((LinkedListNode<T> node) => node.Value);
        this.values = new HashSet<LinkedListNode<T>>(mappedEqualityComparer);
        this.queue = new LinkedList<T>();
    }

    public int Count => values.Count;
    /// <summary>
    /// Puts the specified item back in the queue if it was already in there.
    /// </summary>
    /// <returns>whether the item was already in the queue.</returns>
    public bool Touch(T item)
    {
        if (values.TryGetValue(new LinkedListNode<T>(item), out var value))
        {
            queue.Remove(value);
            queue.AddLast(value);
            return true;
        }
        return false;
    }
    public bool TouchOrAdd(T item)
    {
        if (!Touch(item))
        {
            Add(item);
            return false;
        }
        return true;
    }
    public T Pop()
    {
        var result = queue.First;
        if (result == null)
        {
            throw new Exception("Queue empty");
        }
        queue.RemoveFirst();
        values.Remove(result);
        return result.Value;
    }

    public void Add(T item)
    {
        Contract.Requires(!values.Contains(new LinkedListNode<T>(item)));

        queue.AddLast(item);
        values.Add(queue.Last!);
    }
    public bool Contains(T item)
    {
        return values.Contains(new LinkedListNode<T>(item));
    }
}
