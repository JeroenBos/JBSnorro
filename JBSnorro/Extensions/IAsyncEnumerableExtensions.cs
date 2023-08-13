using System.Collections.Generic;
using System.Diagnostics.Metrics;
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
}
