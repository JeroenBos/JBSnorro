using JBSnorro.Diagnostics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

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

    /// <summary>
    /// Wraps the specified ValueTask in a Task.
    /// </summary>
    public static Task WrapInTask<T>(this in ConfiguredValueTaskAwaitable<T> task)
    {
        var tcs = new TaskCompletionSource();
        task.GetAwaiter().OnCompleted(tcs.SetResult);
        return tcs.Task;
    }

    /// <summary>
    /// This will buffer the elements yielded by the source until either the specified capacity has been reached, or until the source is blocked, as defined by fetching the next element taking longer than <paramref name="blocked_ms"/>.
    /// </summary>
    /// <param name="capacity">The maximum number of elements to be returned by the list.</param>
    /// <param name="blocked_ms">The number of milliseconds to wait for an item element before yielding the current buffer (if non-empty).</param>
    /// <param name="yield_every_ms">The maximum number of milliseconds in between yields of the buffer (if non-empty).</param>
    /// <returns>Any yielded <c>List&lt;T&gt;</c> will be reused.</returns>
    /// <see href="https://stackoverflow.com/a/74201074/308451"/>
    public static async IAsyncEnumerable<List<T>> Buffer<T>(this IAsyncEnumerable<T> source, int capacity, int blocked_ms = 10, int yield_every_ms = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Contract.Requires(source is not null);
        Contract.Requires(capacity > 0);
        Contract.Requires(blocked_ms >= 0);

        var maxNones = capacity + (blocked_ms == 0 ? 1 : (int)Math.Ceiling(yield_every_ms / (double)blocked_ms));
        // a None in the channel significies a waiting time longer than `blocked_ms`
        var channel = Channel.CreateBounded<Option<T>>(new BoundedChannelOptions(capacity + maxNones)
        {
            SingleWriter = true,
            SingleReader = true,
        });
        using CancellationTokenSource completionCts = new();
        var stopwatch = Stopwatch.StartNew();
        int get_ms_until_next_None()
        {
            long untilYield = yield_every_ms - stopwatch.ElapsedMilliseconds;
            if (untilYield < -blocked_ms)
            {
                return int.MaxValue;
            }
            else
            {
                return (int)Math.Max(0, Math.Min(blocked_ms, untilYield));
            }
        }
        Task producer = Task.Run(async () =>
        {
            try
            {
                var enumerator = source.WithCancellation(completionCts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                while (true)
                {
                    var moveNextTask = enumerator.MoveNextAsync();
                    if (!moveNextTask.GetAwaiter().IsCompleted)
                    {
                        var moveNextTaskWrapper = moveNextTask.WrapInTask();
                        while (true)
                        {
                            int ms_until_next_None = get_ms_until_next_None();
                            await Task.WhenAny(moveNextTaskWrapper, Task.Delay(ms_until_next_None));
                            if (!moveNextTask.GetAwaiter().IsCompleted)
                            {
                                await channel.Writer.WriteAsync(Option<T>.None).ConfigureAwait(false);
                            }
                            else
                            {
                                break;
                            }
                        }
                        await moveNextTaskWrapper;
                    }
                    bool hasNext = await moveNextTask;
                    if (!hasNext)
                    {
                        break;
                    }
                    await channel.Writer.WriteAsync(enumerator.Current).ConfigureAwait(false);
                }
            }
            catch (ChannelClosedException) { }
            finally { channel.Writer.TryComplete(); }
        });

        var buffer = new List<T>(capacity);
        try
        {
            await foreach (Option<T> item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.HasValue)
                {
                    buffer.Add(item.Value);
                    if (buffer.Count == capacity)
                    {
                        goto Yield;
                    }
                }
                if (buffer.Count != 0 && (channel.Reader.Count == 0 || stopwatch.ElapsedMilliseconds >= yield_every_ms))
                {
                    goto Yield;
                }
                else
                {
                    // in this case the producer had to wait for blocked_ms, but it isn't the bottleneck; the consumer of this buffer is the bottleneck
                    // so keep filling the buffer with already produced items
                    continue;
                }

            Yield:
                yield return buffer;
                buffer.Clear();
                stopwatch.Reset();
                cancellationToken.ThrowIfCancellationRequested();
            }
            await producer.ConfigureAwait(false); // Propagate possible source error
        }
        finally
        {
            // Prevent fire-and-forget in case the enumeration is abandoned
            if (!producer.IsCompleted)
            {
                completionCts.Cancel();
                channel.Writer.TryComplete();
                await Task.WhenAny(producer).ConfigureAwait(false);
            }
        }
    }
}
