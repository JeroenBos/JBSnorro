using System.Runtime.CompilerServices;

namespace JBSnorro.Extensions;

public static class IAsyncEnumerableExtensions
{
    /// <summary>
    /// Skips the first <paramref name="count"/> elements of the specified sequence.
    /// </summary>
    public static async IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> sequence, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        int skippedCount = 0;
        await using var enumerator = sequence.GetAsyncEnumerator(cancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            if (skippedCount >= count)
            {
                yield return enumerator.Current;
            }
            else
            {
                skippedCount++;
            }
        }
    }
    public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> sequence, CancellationToken cancellationToken = default)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));

        var result = new List<T>();
        await using var enumerator = sequence.GetAsyncEnumerator(cancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            result.Add(enumerator.Current);
        }
        return result;
    }
    public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> sequence, Func<TSource, IEnumerable<TResult>> selector, CancellationToken cancellationToken = default)
    {
        return sequence.SelectMany((element, i) => selector(element), cancellationToken);
    }
    public static async IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> sequence, Func<TSource, int, IEnumerable<TResult>> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var enumerator = sequence.GetAsyncEnumerator(cancellationToken);
        int i = 0;
        while (await enumerator.MoveNextAsync())
        {
            foreach (var value in selector(enumerator.Current, i))
            {
                yield return value;
            }
            i++;
        }
    }
    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> that yields everytime <paramref name="yield"/> is called.
    /// </summary>
    /// <param name="yield">A function to be called that triggers the returned <see cref="IAsyncEnumerable{T}"/> to yield. </param>
    /// <param name="duration"> A length of time for which the async enumerable will run. </param>
    /// <remarks>I'm sure RX extensions has something like this, but whatever. </remarks>
    public static IAsyncEnumerable<object?> Create(out Action yield, TimeSpan duration)
    {
        var result = Create(out yield, out var dispose);
        Task.Delay(duration).ContinueWith(t => dispose());
        return result;
    }
    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> that yields everytime <paramref name="yield"/> is called.
    /// </summary>
    /// <param name="yield">A function to be called that triggers the returned <see cref="IAsyncEnumerable{T}"/> to yield. </param>
    /// <param name="dispose"> A function that terminates this loop. </param>
    /// <remarks>I'm sure RX extensions has something like this, but whatever. </remarks>
    public static IAsyncEnumerable<object?> Create(out Action yield, out Action dispose)
    {
        object _lock = new object();
        var reference = new Reference<TaskCompletionSource<bool>>();
        reference.Value = new TaskCompletionSource<bool>();

        yield = () => Yield(true);
        dispose = () => Yield(false);
        return Loop();

        void Yield(bool result)
        {
            lock (_lock)
            {
                if (!reference!.Value!.Task.IsCompleted)
                {
                    reference.Value.SetResult(result);
                }
            }
        }

        async IAsyncEnumerable<object?> Loop()
        {
            while (await reference.Value.Task)
            {
                yield return null;
                lock (_lock)
                {
                    reference.Value = new TaskCompletionSource<bool>();
                }
            }
        }
    }
}
