using JBSnorro;
using JBSnorro.Algorithms;
using JBSnorro.Collections;
using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Geometry;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using static JBSnorro.Global;

namespace JBSnorro;

/// <summary> Represents the conventional coding patterns of having a function that can retrieve something (via an out parameter) and returns whether it successfully retrieved something. </summary>
public delegate bool TryGetDelegate<TKey, TValue>(TKey key, out TValue value);

public static class EnumerableExtensions
{
    public static IEnumerable<T> OnDisposal<T>(this IEnumerable<T> sequence, Action onDisposal)
    {
        return new EnumerableWithActionOnDisposal<T>(sequence, onDisposal);
    }
    private sealed class EnumerableWithActionOnDisposal<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly IEnumerable<T> sequence;
        private IEnumerator<T>? enumerator;
        private readonly Action onDisposal;

        public EnumerableWithActionOnDisposal(IEnumerable<T> sequence, Action onDisposal)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(onDisposal != null);

            this.sequence = sequence;
            this.onDisposal = onDisposal;
        }

        public T Current => enumerator != null ? enumerator.Current : throw new InvalidOperationException();
        public bool MoveNext()
        {
            if (this.enumerator == null)
            {
                this.enumerator = this.sequence.GetEnumerator();
            }
            return this.enumerator.MoveNext();
        }
        public void Dispose()
        {
            try
            {
                enumerator?.Dispose();
            }
            finally
            {
                onDisposal.Invoke();
            }
        }


        object? IEnumerator.Current => this.Current;
        public void Reset() => throw new InvalidOperationException();
        public IEnumerator<T> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary> Returns whether the specified sequence is empty, ensuring it is enumerated over only once. </summary>
    public static bool IsEmpty<T>(ref IEnumerable<T> sequence)
    {
        Contract.Requires(sequence != null);

        if (sequence is ICollection collection)
        {
            return collection.Count == 0;
        }

        var enumerator = sequence.GetEnumerator();
        bool isEmpty;
        try
        {
            isEmpty = !enumerator.MoveNext();
        }
        catch
        {
            enumerator.Dispose();
            throw;
        }
        sequence = enumerator.ToEnumerable(includeCurrent: isEmpty ? (bool?)null : true);
        return isEmpty;
    }



    /// <summary> Calls the specified callback when the specified enumerable has been enumerated over. </summary>
    public static IEnumerable<T> ContinueWith<T>(this IEnumerable<T> source, Action callback)
    {
        Contract.Requires(source != null);
        Contract.Requires(callback != null);

        IEnumerable<T> Enumerate()
        {
            foreach (T element in source)
                yield return element;
            callback();
        }

        return Enumerate();
    }
    /// <summary>
    /// Copies all elements of the source to <paramref name="destination"/> at the specified <paramref name="destinationStartIndex"/>.
    /// </summary>
    public static void CopyTo<T>(this IReadOnlyList<T> source, T[] destination, int destinationStartIndex)
    {
        Contract.Requires(source != null);
        Contract.Requires(destination != null);
        Contract.Requires(destinationStartIndex + source.Count <= destination.Length);

        if (source is IList<T> ilist)
        {
            ilist.CopyTo(destination, destinationStartIndex);
        }
        else
        {
            for (int i = 0; i < source.Count; i++)
            {
                destination[i + destinationStartIndex] = source[i];
            }
        }
    }

    /// <summary> Determines whether a sequence starts with the specified sequence according to a specified equality comparer. </summary>
    /// <param name="source"> The sequence to check whether its starts with <paramref name="startSequence"/>. </param>
    /// <param name="startSequence"> The sequence to look for in <paramref name="source"/>. </param>
    /// <param name="equalityComparer"> The equality comparer determinign equality between elements of the two sequences. Specify null to use the default equality comparer. </param>
    public static bool StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> startSequence, Func<T, T, bool>? equalityComparer = null)
    {
        Contract.Requires(source != null);
        Contract.Requires(startSequence != null);

        equalityComparer ??= EqualityComparer<T>.Default.Equals;

        using (var sequenceEnumerator = source.GetEnumerator())
        {
            foreach (var startElement in startSequence)
            {
                if (!sequenceEnumerator.MoveNext())
                {
                    return false;
                }
                if (!equalityComparer(sequenceEnumerator.Current, startElement))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary> Returns a selected object for each element in the specified sequence that matches the given predicate. </summary>
    /// <param name="source"> The sequence to apply the predicate on. </param>
    /// <param name="predicate"> A function filtering sequence elements. </param>
    /// <param name="selector"> A function that selects an object from a filtered sequence element and the index in the unfiltered sequence. </param>
    public static IEnumerable<TResult> WhereSelect<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, int, TResult> selector)
    {
        Contract.Requires(source != null);
        Contract.Requires(predicate != null);
        Contract.Requires(selector != null);

        return WhereSelect(source, (sourceElement, sourceIndex) => predicate(sourceElement), selector);
    }
    /// <summary> Like <code><paramref name="source"/>.Where(<paramref name="predicate"/>).Select(<paramref name="selector"/>)</code> but where the index supplied to <paramref name="selector"/> is in the original sequence (as opposed to after having applied the predicate. </summary>
    /// <param name="source"> An enumerable to apply a predicate and transformation on. </param>
    /// <param name="predicate"> A function to test each source element for a condition. The second parameter of the function represents the index of the in the <paramref name="source"/> element. </param>
    /// <param name="selector"> A transform function to apply to each source element. The second parameter of the function represents the index of the in the <paramref name="source"/> element. </param>
    public static IEnumerable<TResult> WhereSelect<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate, Func<TSource, int, TResult> selector)
    {
        Contract.Requires(source != null);
        Contract.Requires(predicate != null);
        Contract.Requires(selector != null);

        IEnumerable<TResult> Enumerate()
        {
            foreach (var (element, sourceIndex) in source.WithIndex())
            {
                if (predicate(element, sourceIndex))
                {
                    yield return selector(element, sourceIndex);
                }
            }
        };

        return Enumerate();
    }
    /// <summary> Selects all elements that could successfully be converted using the specified delegate. </summary>
    /// <param name="trySelect"> A delegate attempting to convert its argument, returning whether it succeeded. </param>
    public static IEnumerable<TResult> WhereTrySelect<TSource, TResult>(this IEnumerable<TSource> source, TryGetDelegate<TSource, TResult> trySelect)
    {
        Contract.Requires(source != null);
        Contract.Requires(trySelect != null);

        IEnumerable<TResult> Enumerate()
        {
            foreach (TSource element in source)
            {
                if (trySelect(element, out TResult result))
                {
                    yield return result;
                }
            }
        }

        return Enumerate();
    }

    /// <summary> Sorts the elements of a sequence in ascending order by using a specified comparer. </summary>
    /// <param name="source"> A sequence of values to order. </param>
    /// <param name="comparer"> An <see cref="IComparer{T}"/> to compare elements. </param>
    public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Func<T, T, int> comparer)
    {
        return source.OrderBy<T>(InterfaceWraps.ToComparer(comparer!));
    }
    /// <summary> Sorts the elements of a sequence in ascending order by using a specified comparer. </summary>
    /// <param name="source"> A sequence of values to order. </param>
    /// <param name="comparer"> An <see cref="IComparer{T}"/> to compare elements. </param>
    public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IComparer<T> comparer)
    {
        Contract.Requires(source != null);
        Contract.Requires(comparer != null);

        return source.OrderBy(_ => _, comparer);
    }

    /// <summary> Produces a result for each corresponding element pair that matches the specified predicate. 
    /// This entails that the resulting sequence is commensurate with the smallest specified sequence. </summary>
    /// <param name="firstSource"> The first sequence to merge. </param>
    /// <param name="secondSource"> The second sequence to merge. </param>
    /// <param name="predicate"> A function filtering pairs from the two sequences. </param>
    /// <param name="selector"> A function that specified how to merge the elements from the two sequences. </param>
    public static IEnumerable<TResult> ZipWhere<T, U, TResult>(this IEnumerable<T> firstSource, IEnumerable<U> secondSource, Func<T, U, bool> predicate, Func<T, U, TResult> selector)
    {
        Contract.Requires(firstSource != null);
        Contract.Requires(secondSource != null);
        Contract.Requires(predicate != null);
        Contract.Requires(selector != null);

        IEnumerable<TResult> Enumerate()
        {
            using (var tEnumerator = firstSource.GetEnumerator())
            using (var uEnumerator = secondSource.GetEnumerator())
            {
                while (tEnumerator.MoveNext() && uEnumerator.MoveNext())
                {
                    if (predicate(tEnumerator.Current, uEnumerator.Current))
                    {
                        yield return selector(tEnumerator.Current, uEnumerator.Current);
                    }
                }
            }
        }

        return Enumerate();
    }

    /// <summary> Applies a specified function the first element of two sequences, and subsequently a function to the remaining corresponding elements of two sequences, producing a sequence of the results.
    /// This entails that the resulting sequence is commensurate with the smallest specified sequence. </summary>
    /// <param name="firstSource"> The first sequence to merge. </param>
    /// <param name="secondSource"> The second sequence to merge. </param>
    /// <param name="selector"> A function that specified how to merge the elements from the two sequences. </param>
    /// <param name="selectorForFirstPair"> A function filtering pairs from the two sequences. </param>
    public static IEnumerable<TResult> Zip<T, U, TResult>(this IEnumerable<T> firstSource, IEnumerable<U> secondSource, Func<T, U, TResult> selector, Func<T, U, TResult> selectorForFirstPair)
    {
        Contract.Requires(firstSource != null);
        Contract.Requires(secondSource != null);
        Contract.Requires(selector != null);
        Contract.Requires(selectorForFirstPair != null);

        IEnumerable<TResult> Enumerate()
        {
            using (var tEnumerator = firstSource.GetEnumerator())
            using (var uEnumerator = secondSource.GetEnumerator())
            {
                if (tEnumerator.MoveNext() && uEnumerator.MoveNext())
                {
                    yield return selectorForFirstPair(tEnumerator.Current, uEnumerator.Current);
                }
                else
                {
                    yield break;
                }
                while (tEnumerator.MoveNext() && uEnumerator.MoveNext())
                {
                    yield return selector(tEnumerator.Current, uEnumerator.Current);
                }
            }
        }
        return Enumerate();
    }
    /// <summary> Creates and populates a dictionary with the specified pairs, and with the default equality comparer. </summary>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> initialCollection) where TKey : notnull
    {
        return initialCollection.ToDictionary(equalityComparer: EqualityComparer<TKey>.Default);
    }
    /// <summary> Creates and populates a dictionary with the specified pairs, and with the specified equality comparer. </summary>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> initialCollection, IEqualityComparer<TKey>? equalityComparer) where TKey : notnull
    {
        Contract.Requires(initialCollection != null);
        Contract.Requires(equalityComparer != null);

        var result = new Dictionary<TKey, TValue>(equalityComparer);
        foreach (var keyValuePair in initialCollection)
        {
            result.Add(keyValuePair.Key, keyValuePair.Value);
        }
        return result;
    }
    /// <summary> Creates and populates a dictionary from the specified keys and equality comparer. </summary>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue> valueSelector, IEqualityComparer<TKey>? equalityComparer = null) where TKey : notnull
    {
        Contract.Requires(keys != null);
        Contract.Requires(valueSelector != null);

        var result = new Dictionary<TKey, TValue>(equalityComparer ?? EqualityComparer<TKey>.Default);
        foreach (TKey key in keys)
        {
            result.Add(key, valueSelector(key));
        }
        return result;
    }

    /// <summary> Returns two elements at specified indices in a sequence. </summary>
    /// <param name="index1"> The index in the specified sequence of the first element to retrieve. Must be in range of the sequence. </param>
    /// <param name="index2"> The index in the specified sequence of the second element to retrieve. May be -1 to indicate that the last element should be retrieved. </param>
    public static (T, T) ElementsAt<T>(this IEnumerable<T> source, int index1, int index2)
    {
        Contract.Requires(source != null);
        Contract.Requires(0 <= index1);
        Contract.Requires(0 <= index2 || index2 == -1);
        Contract.Requires(index1 < index2 || index2 == -1);

        //beforehand check of bounds:
        if (source.TryGetNonEnumeratedCount(out int count) && count < Math.Max(index1, index2))
            throw new IndexOutOfRangeException();

        T? element1 = default(T);
        T? element2 = default(T);

        int i = 0;
        foreach (T element in source)
        {
            if (i == index1)
                element1 = element;
            else if (i == index2)
            {
                element2 = element;
                break;
            }
            if (index2 == -1)
                element2 = element;
        }

        //afterhand check of bounds in case the first check wasn't performed
        if (i < Math.Max(index1, index2))
            throw new IndexOutOfRangeException();

        return (element1, element2)!;
    }
    /// <summary> Searches for the specified sequence and returns the indices of all occurrences within the specified source. Can be empty. </summary>
    /// <param name="source"> A sequence in which to search for the items. </param>
    /// <param name="items"> The items to look for in the source. Cannot be empty. </param>
    /// <param name="equalityComparer"> A comparer determining whether an item to look for matches a source element. </param>
    public static IEnumerable<int> IndicesOf<T, U>(this IEnumerable<T> source, IEnumerable<U> items, Func<T, U, bool> equalityComparer)
    {
        Contract.Requires(source != null);
        Contract.Requires(items != null);
        Contract.RequiresForAny(ref items, _ => true);
        Contract.Requires(equalityComparer != null);

        IEnumerable<int> Enumerate()
        {
            int i = IndexOf(source, items, equalityComparer);
            while (i != -1)
            {
                yield return i;
                source = source.Skip(i + 1); //BUG: This could be a bug if there are overlapping occurrences in source
                i = IndexOf(source, items, equalityComparer);
            }
        }
        return Enumerate();
    }
    /// <summary> Searches for the specified sequence and returns the index of the first occurrence within the specified source; or -1 otherwise. </summary>
    /// <param name="source"> A sequence in which to search for the items. </param>
    /// <param name="items"> The items to look for in the source. Cannot be empty. </param>
    /// <param name="equalityComparer"> A comparer determining whether an item to look for matches a source element. </param>
    public static int IndexOf<T, U>(this IEnumerable<T> source, IEnumerable<U> items, Func<T, U, bool> equalityComparer)
    {
        Contract.Requires(source != null);
        Contract.Requires(items != null);
        Contract.Requires(equalityComparer != null);

        var sequence = (items as IList<U>) ?? items.ToList();
        Contract.Requires(sequence.Count != 0, $"{nameof(items)} cannot be empty. ");

        using (var cachedSource = new LazyReadOnlyList<T>(source))
        {
            for (int startIndex = 0; !cachedSource.FullyCached || startIndex + sequence.Count <= cachedSource.CachedCount; startIndex++)
            {
                if (sequenceIsAt(startIndex))
                {
                    return startIndex;
                }
            }

            bool sequenceIsAt(int startIndex)
            {
                for (int i = 0; i < sequence.Count; i++)
                {
                    if (!cachedSource.TryGetAt(startIndex + i, out T? element))
                    {
                        return false;
                    }
                    if (!equalityComparer(element, sequence[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        return -1;
    }
    /// <summary> Appends the specified element to the specified sequence if the element is not null. </summary>
    [DebuggerHidden]
    public static IEnumerable<T> ConcatIfNotNull<T>(this IEnumerable<T> source, T? element) where T : class
    {
        Contract.Requires(source != null);

        if (element == null)
        {
            return source;
        }
        return source.Concat(element);
    }
    /// <summary> Returns the original sequence, appended with the specified item if the original sequence doesn't already end on that item. </summary>
    public static IEnumerable<T> ConcatIfNotLastElement<T>(this IEnumerable<T> sequence, T item, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence != null);

        equalityComparer ??= EqualityComparer<T>.Default;

        Option<T> lastElement = Option<T>.None;
        foreach (T element in sequence)
        {
            lastElement = element;
            yield return element;
        }

        if (!lastElement.HasValue || !equalityComparer.Equals(lastElement.Value, item))
        {
            yield return item;
        }
    }

    /// <summary> Determines whether all elements of a sequence satisfy a condition. </summary>
    /// <param name="source"> An <see cref="IEnumerable{T}"/> to filter. </param>
    /// <param name="predicate"> A function to test each source element for a condition; the second parameter of the function represents the index of the source element. </param>
    public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        Contract.Requires(source != null);
        Contract.Requires(predicate != null);

        return source.WithIndex()
                     .All(tuple => predicate(tuple.Element, tuple.Index));
    }

    /// <summary> Gets the first occurrence in the specified sequence that equals the specified	item according to the specified equality comparer. </summary>
    /// <param name="source"> The sequence to look in for the item. </param>
    /// <param name="item"> The item to look for. </param>
    /// <param name="equalityComparer"> The equality comparer to determine equality. Specify null to use the default equality comparer. </param>
    public static T? Find<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(source != null);

        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        return source.FirstOrDefault(other => equalityComparer.Equals(item, other));
    }
    /// <summary> Applies a specified function to the corresponding elements of three sequences, producing a sequence of the results. 
    /// This entails that the resulting sequence is commensurate with the smallest specified sequence. </summary>
    /// <param name="firstSequence"> The first sequence to merge. </param>
    /// <param name="secondSequence"> The second sequence to merge. </param>
    /// <param name="thirdSequence"> The third sequence to merge. </param>
    /// <param name="resultSelector"> A function that specifies how to merge the elements from the three sequences. </param>
    public static IEnumerable<TResult> Zip<T, U, V, TResult>(this IEnumerable<T> firstSequence, IEnumerable<U> secondSequence, IEnumerable<V> thirdSequence, Func<T, U, V, TResult> resultSelector)
    {
        Contract.Requires(firstSequence != null);
        Contract.Requires(secondSequence != null);
        Contract.Requires(thirdSequence != null);
        Contract.Requires(resultSelector != null);

        IEnumerable<TResult> Enumerate()
        {
            using (var tEnumerator = firstSequence.GetEnumerator())
            using (var uEnumerator = secondSequence.GetEnumerator())
            using (var vEnumerator = thirdSequence.GetEnumerator())
            {
                while (tEnumerator.MoveNext() && uEnumerator.MoveNext() && vEnumerator.MoveNext())
                {
                    yield return resultSelector(tEnumerator.Current, uEnumerator.Current, vEnumerator.Current);
                }
            }
        }
        return Enumerate();
    }
    /// <summary> Applies a specified function to the corresponding elements of four sequences, producing a sequence of the results. 
    /// This entails that the resulting sequence is commensurate with the smallest specified sequence. </summary>
    /// <param name="firstSequence"> The first sequence to merge. </param>
    /// <param name="secondSequence"> The second sequence to merge. </param>
    /// <param name="thirdSequence"> The third sequence to merge. </param>
    /// <param name="fourthSequence"> The third sequence to merge. </param>
    /// <param name="resultSelector"> A function that specifies how to merge the elements from the four sequences. </param>
    public static IEnumerable<TResult> Zip<T, U, V, W, TResult>(this IEnumerable<T> firstSequence,
                                                                     IEnumerable<U> secondSequence,
                                                                     IEnumerable<V> thirdSequence,
                                                                     IEnumerable<W> fourthSequence,
                                                                     Func<T, U, V, W, TResult> resultSelector)
    {
        Contract.Requires(firstSequence != null);
        Contract.Requires(secondSequence != null);
        Contract.Requires(thirdSequence != null);
        Contract.Requires(fourthSequence != null);
        Contract.Requires(resultSelector != null);

        IEnumerable<TResult> Enumerate()
        {
            using (var tEnumerator = firstSequence.GetEnumerator())
            using (var uEnumerator = secondSequence.GetEnumerator())
            using (var vEnumerator = thirdSequence.GetEnumerator())
            using (var wEnumerator = fourthSequence.GetEnumerator())
            {
                while (tEnumerator.MoveNext() && uEnumerator.MoveNext() && vEnumerator.MoveNext())
                {
                    yield return resultSelector(tEnumerator.Current, uEnumerator.Current, vEnumerator.Current, wEnumerator.Current);
                }
            }
        }
        return Enumerate();
    }
    /// <summary> Determines whether two sequences are equal according to an equality comparer. </summary>
    /// <param name="firstSequence"> An <see cref="IEnumerable{T}"/> to compare to <paramref name="secondSequence"/>. </param>
    /// <param name="secondSequence"> An <see cref="IEnumerable{T}"/> to compare to <paramref name="firstSequence"/>. </param>
    /// <param name="equalityComparer"> An <see cref="IEqualityComparer{T}"/> to use to compare elements. </param>
    [DebuggerHidden]
    public static bool SequenceEqual<T, U>(this IEnumerable<T> firstSequence, IEnumerable<U> secondSequence, Func<T, U, bool> equalityComparer)
    {
        Contract.Requires(firstSequence != null);
        Contract.Requires(secondSequence != null);
        Contract.Requires(equalityComparer != null);

        using (IEnumerator<T> enumerator1 = firstSequence.GetEnumerator())
        using (IEnumerator<U> enumerator2 = secondSequence.GetEnumerator())
        {
            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext() || !equalityComparer(enumerator1.Current, enumerator2.Current))
                {
                    return false;
                }
            }
            if (enumerator2.MoveNext())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary> Determines whether two sequences are equal by comparing tokens selected for their elements by using a specified <see cref="IEqualityComparer"/>. </summary>
    /// <param name="comparableTokenSelector"> A function to extract a comparable token from an element. </param>
    /// <param name="equalityComparer"> A function to use to compare element tokens. Specify null to use the default. </param>
    public static bool SequenceEqualBy<T, TComparableToken>(this IEnumerable<T> firstSequence,
                                                                 IEnumerable<T> secondSequence,
                                                                 Func<T, TComparableToken> comparableTokenSelector,
                                                                 IEqualityComparer<TComparableToken>? equalityComparer = null)
    {
        Contract.Requires(firstSequence != null);
        Contract.Requires(secondSequence != null);
        Contract.Requires(comparableTokenSelector != null);

        equalityComparer ??= EqualityComparer<TComparableToken>.Default;

        return firstSequence.SequenceEqual(secondSequence, (element1, element2) => equalityComparer.Equals(comparableTokenSelector(element1), comparableTokenSelector(element2)));
    }

    /// <summary> Returns the specified characters concatenated as string. </summary>
    /// <param name="characters"> The characters to create a string from. </param>
    public static string Concat(this IEnumerable<char> characters)
    {
        Contract.Requires(characters != null);

        return new string(characters.ToArray());
    }
    /// <summary> Returns whether the specified sequence of integers is sequential and increasing. </summary>
    /// <param name="source"> The sequence to check for begin sequential. </param>
    public static bool AreSequential(this IEnumerable<int> source)
    {
        return source.AreSequential((a, b) => a + 1 == b);
    }
    /// <summary> Gets whether the specified sequence is sequential according to the specified sequentiality comparer. The empty and singleton sequences are considered sequential. </summary>
    /// <param name="source"> The sequence to be tested for sequentiality. </param>
    /// <param name="areSequential"> The function testing whether two successive elements are sequential. </param>
    public static bool AreSequential<T>(this IEnumerable<T> source, Func<T, T, bool> areSequential)
    {
        Contract.Requires(source != null);
        Contract.Requires(areSequential != null);

        return source.Windowed2()
                     .All(tuple => areSequential(tuple.First, tuple.Second));
    }

    /// <summary> Gets whether the specified sequence is (non-strictly) increasing. </summary>
    public static bool AreIncreasing(this IEnumerable<int> source)
    {
        return source.AreSequential((a, b) => a <= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) increasing. </summary>
    public static bool AreIncreasing(this IEnumerable<ulong> source)
    {
        return source.AreSequential((a, b) => a <= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) increasing. </summary>
    public static bool AreIncreasing(this IEnumerable<float> source)
    {
        return source.AreSequential((a, b) => !float.IsNaN(a) && a <= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) increasing. </summary>
    public static bool AreIncreasing(this IEnumerable<double> source)
    {
        return source.AreSequential((a, b) => !double.IsNaN(a) && a <= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) decreasing. </summary>
    public static bool AreDecreasing(this IEnumerable<int> source)
    {
        return source.AreSequential((a, b) => a >= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) decreasing. </summary>
    public static bool AreDecreasing(this IEnumerable<ulong> source)
    {
        return source.AreSequential((a, b) => a >= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) decreasing. </summary>
    public static bool AreDecreasing(this IEnumerable<float> source)
    {
        return source.AreSequential((a, b) => !float.IsNaN(a) && a >= b);
    }
    /// <summary> Gets whether the specified sequence is (non-strictly) decreasing. </summary>
    public static bool AreDecreasing(this IEnumerable<double> source)
    {
        return source.AreSequential((a, b) => !double.IsNaN(a) && a >= b);
    }
    /// <summary> Determines whether the source and items sequences have the same elements according to a specified equality comparer. 
    /// This includes the number of their occurrences, and is irrespective of order. </summary>
    /// <param name="source"> The sequence to be compared to <paramref name="items"/>. </param>
    /// <param name="items"> The sequence to be compared to <paramref name="source"/>. </param>
    public static bool ContainsSameElements<T>(this IEnumerable<T> source, params T[] items)
    {
        return source.ContainsSameElements(items, (IEqualityComparer<T>?)null);
    }
    /// <summary> Determines whether the source and items sequences have the same elements according to a specified equality comparer. 
    /// This includes the number of their occurrences, and is irrespective of order. </summary>
    /// <param name="source"> The sequence to be compared to <paramref name="items"/>. </param>
    /// <param name="items"> The sequence to be compared to <paramref name="source"/>. </param>
    /// <param name="equalityComparer"> The equality comparer determining equality between elements of both sequences. Specify null to use the default equality comparer. </param>
    public static bool ContainsSameElements<T>(this IEnumerable<T> source, IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
    {
        return ContainsSameElements(source, items, equalityComparer == null ? (Func<T, T, bool>?)null : equalityComparer.Equals);
    }
    /// <summary> Determines whether the source and items sequences have the same elements according to a specified equality comparer. 
    /// This includes the number of their occurrences, and is irrespective of order. </summary>
    /// <param name="source"> The sequence to be compared to <paramref name="items"/>. </param>
    /// <param name="items"> The sequence to be compared to <paramref name="source"/>. </param>
    /// <param name="equalityComparer"> The equality comparer determining equality between elements of both sequences. Specify null to use <code>T.Equals(object)</code>. </param>
    public static bool ContainsSameElements<T, U>(this IEnumerable<T> source, IEnumerable<U> items, Func<T, U, bool>? equalityComparer = null)
    {
        return containsAll(source, items, equalityComparer, requiresToUseAllItems: true);
    }
    /// <summary> Gets whether the specified element contains all items, without using an element twice. Not necessarily in the same order. Assumes no item can equal two elements. </summary>
    /// <param name="source"> The sequence to check whether it contains all elements in <paramref name="items"/>. </param>
    /// <param name="items"> The items to check for presence in the <paramref name="source"/>. </param>
    /// <param name="equalityComparer"> The function determining if an element equals an item. Specify null to use <code>T.Equals(object)</code>. </param>
    public static bool ContainsAll<T, U>(this IEnumerable<T> source, IEnumerable<U> items, Func<T, U, bool>? equalityComparer = null)
    {
        return containsAll(source, items, equalityComparer, requiresToUseAllItems: false);
    }
    private static bool containsAll<T, U>(this IEnumerable<T> source, IEnumerable<U> items, Func<T, U, bool>? equalityComparer, bool requiresToUseAllItems)
    {
        Contract.Requires(source != null);
        Contract.Requires(items != null);

        // beforehand check whether sequences are commensurate
        if (requiresToUseAllItems)
        {
            int? sourceCount = TryGetCount(source);
            int? itemsCount = TryGetCount(items);
            if (sourceCount.HasValue && itemsCount.HasValue && sourceCount != itemsCount)
            {
                return false;
            }
        }

        equalityComparer ??= equalityComparer ?? ((t, u) => ReferenceEquals(t, null) ? ReferenceEquals(u, null) : t.Equals(u));

        var sourceElements = source.ToList(); //PERFORMANCE: used linked list, and not indices but linked list nodes
        foreach (U item in items)
        {
            if (sourceElements.Count == 0)
            {
                return false;
            }

            int i = sourceElements.IndexOf(element => equalityComparer(element, item));
            if (i == -1)
            {
                return false;
            }
            sourceElements.RemoveAt(i);
        }

        if (requiresToUseAllItems)
        {
            return sourceElements.Count == 0;
        }
        else
        {
            return true;
        }


        int? TryGetCount<W>(IEnumerable<W> sequence)
        {
            if (sequence is IReadOnlyCollection<W> list)
            {
                return list.Count;
            }
            else if (sequence is ICollection<W> collection)
            {
                return collection.Count;
            }
            return null;
        }
    }

    /// <summary> Returns the specified sequence where each element is accompanied by its index in the sequence. </summary>
    public static IEnumerable<(T Element, int Index)> WithIndex<T>(this IEnumerable<T> source)
    {
        Contract.Requires(source != null);

        return source.Select((element, index) => (element, index));
    }
    /// <summary> Returns the specified sequence where each element is accompanied by whether it is the first element in the sequence. </summary>
    public static IEnumerable<(T Element, bool IsFirst)> WithIsFirst<T>(this IEnumerable<T> source)
    {
        Contract.Requires(source != null);

        return source.Select((element, index) => (element, index == 0));
    }
    /// <summary> Returns the specified sequence where each element is accompanied by whether it is the first element in the sequence. </summary>
    public static IEnumerable<(T Element, bool IsLast)> WithIsLast<T>(this IEnumerable<T> source)
    {
        Contract.Requires(source != null);

        IEnumerable<(T Element, bool IsLast)> Enumerate()
        {
            using (var enumerator = source.GetEnumerator())
            {
                T previous;
                if (enumerator.MoveNext())
                {
                    previous = enumerator.Current;
                }
                else //specified sequence is empty
                {
                    yield break;
                }
                while (enumerator.MoveNext())
                {
                    yield return (previous, IsLast: false);
                    previous = enumerator.Current;
                }
                yield return (previous, IsLast: true);
            }
        }
        return Enumerate();
    }
    /// <summary> Splits the specified enumerable into multiple enumerables based on a function determining whether to split given two consecutive source elements. </summary>
    /// <param name="source"> The sequence to split into multiple subsequences. </param>
    /// <param name="shouldSplitBetween"> The function determining whether a split should occur between the two consecutive source elements (specified as arguments). </param>
    public static IEnumerable<IList<T>> Split<T>(this IEnumerable<T> source, Func<T, T, bool> shouldSplitBetween)
    {
        Contract.Requires(source != null);
        Contract.Requires(shouldSplitBetween != null);

        //the return type cannot be IEnumerable<IEnumerable<T>>, since then it may seem as though order of enumeration may play some role. 
        IEnumerable<IList<T>> Enumerate()
        {
            List<T> currentSubsequence = new List<T>();
            foreach (var (element, isFirstElement) in source.WithIsFirst())
            {
                if (!isFirstElement && shouldSplitBetween(currentSubsequence.Last(), element))
                {
                    yield return currentSubsequence.ToArray();
                    currentSubsequence.Clear();
                }
                currentSubsequence.Add(element);
            }
            yield return currentSubsequence;
        }
        return Enumerate();
    }

    /// <summary> Returns the specified sequence, where consecutive subsequences are yielded in reverse order. </summary>
    /// <param name="source"> The sequence to yield a modified version of. </param>
    /// <param name="predicate"> The function that determines whether an element belongs to a subsequence that is to be yielded reversely. </param>
    public static IEnumerable<T> ReverseSubsequences<T>(this IList<T> source, Func<T, bool> predicate)
    {
        Contract.Requires(source != null);
        Contract.Requires(predicate != null);

        IEnumerable<T> Enumerate()
        {
            // The value of `subsequenceIndex` when there is currently no subsequence to be reversed
            const int noSubsequence = -1;
            //the index of the first element in the subsequence to be reversed
            int subsequenceIndex = noSubsequence;

            foreach (var (element, elementIndex) in source.WithIndex())
            {
                if (predicate(element))
                {
                    if (subsequenceIndex == noSubsequence)
                    {
                        subsequenceIndex = elementIndex;
                    }
                }
                else if (subsequenceIndex != noSubsequence)
                {
                    for (int reversingIndex = elementIndex - 1; reversingIndex >= subsequenceIndex; reversingIndex--)
                    {
                        yield return source[reversingIndex];
                    }
                    subsequenceIndex = noSubsequence;
                }
                else
                {
                    yield return element;
                }
            }

            //if the last element matches the predicate, then the for-loop terminates without reaching the point to yield the subsequence reversely, in which case it is yielded here
            if (subsequenceIndex != noSubsequence)
            {
                for (int reversingIndex = source.Count - 1; reversingIndex >= subsequenceIndex; reversingIndex--)
                {
                    yield return source[reversingIndex];
                }
            }
        }
        return Enumerate();
    }

    /// <summary> Gives the indices in the specified sequence that are minimal according to the specified comparison function. </summary>
    /// <typeparam name="T"> The elements of the sequence to find the minima of. </typeparam>
    /// <param name="source"> The sequence to find the minima of. Cannot be null or empty. </param>
    /// <param name="comparer"> The function comparing element to determine the minimum. </param>
    public static List<int> IndicesOfMinima<T>(this IEnumerable<T> source, Func<T, T, int> comparer)
    {
        Contract.Requires(source != null);
        Contract.Requires(comparer != null);

        T? minimum = default(T);
        List<int> result = new List<int>();
        int i = 0;
        foreach (T element in source)
        {
            if (i == 0)
            {
                result.Add(i);
                minimum = element;
            }
            else
            {
                int comparisonResult = comparer(element, minimum!);
                if (comparisonResult == 0)
                {
                    result.Add(i);
                }
                else if (comparisonResult < 0)
                {
                    result.Clear();
                    result.Add(i);
                    minimum = element;
                }
                //else continue
            }
            i++;
        }

        if (result.Count == 0)
            throw new ArgumentException("The sequence is empty", "sequence");

        return result;
    }
    /// <summary> Returns the accumulative sequence of the specified doubles. </summary>
    /// <param name="source"> The sequence to accumulate. </param>
    public static IEnumerable<double> Accumulate(this IEnumerable<double> source)
    {
        Contract.Requires(source != null);

        return source.Scan((a, b) => a + b, 0d);
    }
    /// <summary> Returns the accumulative sequence of the specified integers. </summary>
    /// <param name="source"> The sequence to accumulate. </param>
    public static IEnumerable<int> Accumulate(this IEnumerable<int> source)
    {
        Contract.Requires(source != null);

        return source.Scan((a, b) => a + b, 0);
    }

    /// <summary> Concatenates all specified sequences. </summary>
    /// <typeparam name="T"> The type of the sequence elements. </typeparam>
    /// <param name="sources"> The sequences to concatenate. Cannot be or contain null. </param>
    [DebuggerHidden]
    public static IEnumerable<T> Concat<T>(this IEnumerable<IEnumerable<T>> sources)
    {
        EnsureSingleEnumerationDEBUG(ref sources);
        Contract.Requires(sources != null);
        Contract.RequiresForAll(sources, NotNull);

        return _concat(sources);

        [DebuggerHidden]
        IEnumerable<T> _concat(IEnumerable<IEnumerable<T>> sources)
        {
            foreach (var sequence in sources)
                foreach (T element in sequence)
                    yield return element;
        }
    }
    /// <summary> Concatenates all specified sequences. </summary>
    /// <typeparam name="T"> The type of the sequence elements. </typeparam>
    /// <param name="sources"> The sequences to concatenate. Cannot be or contain null. </param>
    [DebuggerHidden]
    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] sources)
    {
        Contract.Requires(sources != null);

        return sources.Concat();
    }

    /// <summary> Returns the specified sequence except the element at the specified index. </summary>
    /// <param name="source"> The sequence to skip an element of. </param>
    /// <param name="indexToSkip"> The index of the element to skip. DMust be nonnegative, but not necessarily within the upper bound of <paramref name="source"/>. </param>
    public static IEnumerable<T> ExceptAt<T>(this IEnumerable<T> source, int indexToSkip)
    {
        return source.Where((element, elementIndex) => elementIndex != indexToSkip);
    }
    /// <summary> Returns the specified sequence except the element at the specified indices. </summary>
    /// <param name="source"> The sequence to skip items of. </param>
    /// <param name="indicesToSkip"> The indices to skip. Must be nonnegative, ordered ascendingly, but not necessarily within the upper bound of <paramref name="source"/>. </param>
    public static IEnumerable<T> ExceptAt<T>(this IEnumerable<T> source, params int[] indicesToSkip)
    {
        Contract.Requires(source != null);
        Contract.Requires(indicesToSkip != null);
        Contract.RequiresWindowed2(indicesToSkip, (first, second) => first != second, $"{nameof(indicesToSkip)} must contain unique elements");
        Contract.RequiresWindowed2(indicesToSkip, (first, second) => first < second, $"{nameof(indicesToSkip)} must be ordered");

        return source.ExceptAt(new SortedList<int>(indicesToSkip));
    }
    /// <summary> Returns the specified sequence except the element at the specified indices. </summary>
    /// <param name="source"> The sequence to skip items of. </param>
    /// <param name="indicesToSkip"> The indices to skip. Must be nonnegative, ordered ascendingly, but not necessarily within the upper bound of <paramref name="source"/>. </param>
    public static IEnumerable<T> ExceptAt<T>(this IEnumerable<T> source, SortedList<int> indicesToSkip)
    {
        Contract.Requires(source != null);
        Contract.Requires(indicesToSkip != null);
        Contract.Requires(indicesToSkip.AreIncreasing());
        Contract.Requires(indicesToSkip.FirstOrDefault() >= 0);

        IEnumerable<T> Enumerate()
        {
            int indexInIndicesToSkip = 0;
            foreach (var (element, elementIndex) in source.WithIndex())
            {
                if (indexInIndicesToSkip == indicesToSkip.Count || elementIndex != indicesToSkip[indexInIndicesToSkip])
                {
                    yield return element;
                }
                else
                {
                    indexInIndicesToSkip++;
                }
            }
        }
        return Enumerate();
    }
    /// <summary> Returns the specified sequence except the elements in the specified range. </summary>
    /// <param name="source"> The sequence to skip an element of. </param>
    /// <param name="rangeToSkip"> The range of the elements to skip. </param>
    public static IEnumerable<T> ExceptAt<T>(this IEnumerable<T> source, Range rangeToSkip)
    {
        if (!source.TryGetNonEnumeratedCount(out int sourceCount))
        {
            Contract.Requires<NotImplementedException>(!rangeToSkip.Start.IsFromEnd);
            Contract.Requires<NotImplementedException>(!rangeToSkip.End.IsFromEnd);
        }

        var range = rangeToSkip.GetOffsetAndLength(sourceCount);
        var min = range.Offset;
        var max = range.Offset + range.Length;
        return source.Where((element, elementIndex) => !(min <= elementIndex && elementIndex < max));
    }

    /// <summary> Returns the specified sequence except the last element. </summary>
    /// <param name="source"> The sequence to skip the last element of. </param>
    public static IEnumerable<T> ExceptLast<T>(this IEnumerable<T> source)
    {
        return source.WithIsLast()
                     .Where(element => !element.IsLast)
                     .Select(tuple => tuple.Element);
    }
    /// <summary> Returns all elements in the specified sequence that are minimal according to the default comparer. </summary>
    /// <param name="source"> The sequence to find the minimal elements of. Can be empty. </param>
    public static List<T> FindMinima<T>(this IEnumerable<T> source) where T : IComparable<T>
    {
        return FindMinima(source, Comparer<T>.Default.Compare);
    }
    /// <summary> Returns all elements in the specified sequence that are minimal according to the specified comparer. </summary>
    /// <param name="source"> The sequence to find the minimal elements of. Can be empty. </param>
    /// <param name="comparer"> A function to compare elements. Can be null. </param>
    public static List<T> FindMinima<T>(this IEnumerable<T> source, Func<T, T, int>? comparer = null)
    {
        Contract.Requires(source != null);
        Contract.Requires(comparer != null
                       || typeof(T).Implements(typeof(IComparable))
                       || typeof(T).Implements(typeof(IComparable<T>)), $"A comparer must be specified or {nameof(T)} must implement either {nameof(IComparable)} or {nameof(IComparable<T>)}");

        comparer ??= Comparer<T>.Default.Compare;

        //finds the minima by finding the maxima of the inverted comparer
        return FindMaxima(source, (a, b) => comparer(b, a));
    }

    /// <summary> Gets the minimal element of the specified sequence; or an alternative if is empty. </summary>
    public static T? MinOrDefault<T>(this IEnumerable<T> source, T? defaultIfEmpty = default(T))
    {
        Contract.Requires(source != null);

        if (IsEmpty(ref source))
        {
            return defaultIfEmpty;
        }
        else
        {
            return source.Min();
        }
    }
    /// <summary> Gets the maximal element of the specified sequence, or an alternative when it is empty. </summary>
    public static T? MaxOrDefault<T>(this IEnumerable<T> sequence, T? defaultIfEmpty = default(T))
    {
        Contract.Requires(sequence != null);

        using (var enumerator = sequence.GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                return defaultIfEmpty;
            }

            T max = enumerator.Current;
            while (enumerator.MoveNext())
            {
                if (Comparer<T>.Default.Compare(max, enumerator.Current) < 0)
                    max = enumerator.Current;
            }
            return max;
        }
    }
    /// <summary> Returns the (first if there exist multiple) minimum in the specified sequence according to comparable keys. </summary>
    /// <param name="sequence"> The sequence to return the minimum of. Must be non-empty. </param>
    /// <param name="keySelector"> The function that selects the comparable key for each element. </param>
    /// <param name="comparer"> A comparer determing the order of the keys. </param>
    public static T MinimumBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> keySelector, Func<TKey, TKey, int>? comparer = null)
    {
        comparer = comparer ?? Comparer<TKey>.Default.Compare;
        Func<TKey, TKey, int> invertedComparer = (a, b) => comparer(b, a);
        return sequence.MaximumBy(keySelector, invertedComparer);
    }

    /// <summary> Returns the (first if there exist multiple) maximum in the specified sequence according to comparable keys. </summary>
    /// <param name="sequence"> The sequence to return the maximum of. Must be non-empty. </param>
    /// <param name="keySelector"> The function that selects the comparable key for each element. </param>
    /// <param name="comparer"> A comparer determing the order of the keys. </param>
    public static T MaximumBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> keySelector, Func<TKey, TKey, int>? comparer = null)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(keySelector != null);

        comparer = comparer ?? Comparer<TKey>.Default.Compare;

        T? max = default(T);
        TKey? maxKey = default(TKey);
        bool first = true;
        foreach (T element in sequence)
        {
            var key = keySelector(element);
            if (first || comparer(maxKey!, key) < 0)
            {
                first = false;
                max = element;
                maxKey = key;
            }
        }
        if (first)
        {
            throw new ArgumentException("The specified sequence was empty", nameof(sequence));
        }
        return max!;
    }

    /// <summary> Returns the maxima in the specified sequence according to comparable keys. </summary>
    /// <param name="sequence"> The sequence to return the maxima of. </param>
    /// <param name="keySelector"> The function that selects the comparable key for each element. </param>
    public static List<T> MaximaBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
    {
        List<T> result = new List<T>();
        TKey? max = default(TKey);
        bool first = true;
        foreach (T element in sequence)
        {
            var potentialMax = keySelector(element);
            if (first || potentialMax.CompareTo(max) >= 0)
            {
                max = potentialMax;
                result.Add(element);
            }
            if (first)
            {
                first = false;
            }
        }
        //empty sequence returns empty list...
        return result;
    }
    /// <summary> Returns the minima in the specified sequence according to comparable keys. </summary>
    /// <param name="sequence"> The sequence to return the minima of. </param>
    /// <param name="keySelector"> The function that selects the comparable key for each element. </param>
    public static List<T> MinimaBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
    {
        return MinimaByMany(sequence, t => keySelector(t).ToSingleton());
    }
    public static List<T> MinimaByMany<T, TKey>(this IEnumerable<T> sequence, Func<T, IEnumerable<TKey>> keySelector) where TKey : IComparable<TKey>
    {
        List<T> result = new List<T>();
        TKey? min = default(TKey);
        bool first = true;
        foreach (T element in sequence)
        {
            foreach (var potentialMin in keySelector(element))
            {
                if (first || potentialMin.CompareTo(min) < 0)
                {
                    min = potentialMin;
                    result.Clear();
                    result.Add(element);
                    first = false;
                }
                else if (potentialMin.CompareTo(min) == 0)
                {
                    result.Add(element);
                }
            }
        }
        //if (first) throw new ArgumentException("the specified sequence was empty");
        //empty sequence returns empty list...
        return result;
    }
    public static List<T> MaximaByMany<T, TKey>(this IEnumerable<T> sequence, Func<T, IEnumerable<TKey>> keySelector) where TKey : IComparable<TKey>
    {
        List<T> result = new List<T>();
        TKey? max = default(TKey);
        bool first = true;
        foreach (T element in sequence)
        {
            foreach (var potentialMax in keySelector(element))
            {
                if (first || potentialMax.CompareTo(max) > 0)
                {
                    max = potentialMax;
                    result.Clear();
                    result.Add(element);
                    first = false;
                }
                else if (potentialMax.CompareTo(max) == 0)
                {
                    result.Add(element);
                }
            }
        }
        //if (first) throw new ArgumentException("the specified sequence was empty");
        //empty sequence returns empty list...
        return result;
    }
    /// <summary> Returns the indices of all elements in enumerable that are maximal according to the default comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximum elements of. Cannot be null. Can be empty. </param>
    public static SortedList<int> FindIndicesOfMinima<T>(this IEnumerable<T> enumerable)
    {
        return FindIndicesOfMinima(enumerable, Comparer<T>.Default.Compare);
    }
    /// <summary> Returns the indices of all elements in enumerable that are maximal according to the specified comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximum elements of. Cannot be null. Can be empty. </param>
    /// <param name="comparer"> The comparer returning a positive (negative) value if the first argument is larger (smaller) than the second argument, 
    /// and zero when they are comparable. Cannot be null. </param>
    public static SortedList<int> FindIndicesOfMinima<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer)
    {
        return FindIndicesOfMaxima(enumerable, (a, b) => comparer(b, a)); //finds minima by finding maxima of inverted comparer
    }

    /// <summary> Returns all elements in enumerable that are maximal according to the default comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximal elements of. Cannot be null. Can be empty. </param>
    public static List<T> FindMaxima<T>(this IEnumerable<T> enumerable)
    {
        return FindMaxima(enumerable, Comparer<T>.Default.Compare);
    }
    /// <summary> Returns all elements in enumerable that are maximal according to a specified comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximal elements of. Cannot be null. Can be empty. </param>
    /// <param name="comparer"> The comparer returning a positive (negative) value if the first argument is larger (smaller) than the second argument, 
    /// and zero when they are comparable. Cannot be null. </param>
    /// <returns> Returns the elements { e } such that comparer(e, x) &lt;= 0 for all x in enumerable and for all e in result. </returns>
    public static List<T> FindMaxima<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer)
    {
        if (enumerable == null) throw new ArgumentNullException("enumerable");
        if (comparer == null) throw new ArgumentNullException("comparer");

        List<T> maxima = new List<T>();

        foreach (T element in enumerable)
        {
            if (maxima.Count == 0)
            {
                //i.e. the first element in enumerable
                maxima.Add(element);
                continue;
            }
            else
            {
                //i.e. there is already a potential maximum. 
                //If the current element is larger, discard the current potential maximums and substitute them by the current.
                //If the current element is smaller, ignore it.
                //If the current element is comparable, add it to the list.
                int comparisonResult = comparer(element, maxima[0]);
                if (comparisonResult > 0)
                {
                    maxima.Clear();
                    maxima.Add(element);
                }
                else if (comparisonResult < 0)
                {
                    continue;
                }
                else
                {
                    maxima.Add(element);
                }
            }
        }

        return maxima;
    }
    /// <summary> Returns the indices of all elements in enumerable that are maximal according to the default comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximum elements of. Cannot be null. Can be empty. </param>
    public static SortedList<int> FindIndicesOfMaxima<T>(this IEnumerable<T> enumerable)
    {
        return FindIndicesOfMaxima(enumerable, Comparer<T>.Default.Compare);
    }
    /// <summary> Returns the indices of all elements in enumerable that are maximal according to the specified comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable of which to to yield the maximum elements of. Cannot be null. Can be empty. </param>
    /// <param name="comparer"> The comparer returning a positive (negative) value if the first argument is larger (smaller) than the second argument, 
    /// and zero when they are comparable. Cannot be null. </param>
    public static SortedList<int> FindIndicesOfMaxima<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer)
    {
        if (enumerable == null) throw new ArgumentNullException("enumerable");
        if (comparer == null) throw new ArgumentNullException("comparer");

        //The following two lines are the same as that below, but slower. 
        //In case DRY should prevail, substitute this method body by those two lines
        //List<T> cache = enumerable.ToList();
        //return Enumerable.Range(0, cache.Count).FindMaxima((a, b) => comparer(cache[a], cache[b]));

        SortedList<int> result = new SortedList<int>(); //comparer by normal index order.
        T? cachedElement = default(T);
        int i = 0;

        foreach (T element in enumerable)
        {
            if (i == 0)
            {
                cachedElement = element;
                result.Add(0);
            }
            else
            {
                int comparisonResult = comparer(element, cachedElement!);
                if (comparisonResult > 0)
                {
                    result.Clear();
                    result.Add(i);
                    cachedElement = element;
                }
                else if (comparisonResult == 0)
                {
                    result.Add(i);
                }
            }
            i++;
        }

        return result;
    }
    public static IEnumerable<int> FindIndicesOfMaxima<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer, T maximum)
    {
        EnsureSingleEnumerationDEBUG(ref enumerable);
        Contract.Requires(enumerable != null);
        Contract.Requires(comparer != null);

        int i = 0;
        foreach (T element in enumerable)
        {
            if (comparer(element, maximum) == 0)
                yield return i;
            i++;
        }
    }
    /// <summary> Finds the index of the first maximum in a sequence, specified by a specific comparer. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The sequence of elements to determine the maximum of. </param>
    /// <param name="comparer"> A function comparing two elements, stating which is larger. If the first argument is larger than the second, a positive number is to returned. </param>
    public static int FindIndexOfMaximum<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer)
    {
        Contract.Requires(enumerable != null);
        Contract.Requires(comparer != null);

        const int initialIndexValue = -1;
        T? max = default(T);
        int indexOfMax = initialIndexValue;
        int i = 0;
        foreach (T element in enumerable)
        {
            if (indexOfMax == initialIndexValue || comparer(element, max!) > 0)
            {
                max = element;
                indexOfMax = i;
            }
            i++;
        }

        Contract.Ensures(indexOfMax >= 0);
        return indexOfMax;
    }
    /// <summary> Inserts nodes between all elements in the specified sequence based on the two elements between which nodes may be inserted. This method does not append or prepend nodes to the enumerable. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence to insert nodes into. </param>
    /// <param name="nodesToInsert"> A delegate getting the nodes to be inserted between the first and second argument. The returned enumerable may be empty, but not null. </param>
    public static IEnumerable<T> Insert<T>(this IEnumerable<T> sequence, Func<T, T, IEnumerable<T>> nodesToInsert)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        if (nodesToInsert == null) throw new ArgumentNullException(nameof(nodesToInsert));

        T? last = default(T);
        bool first = true;
        foreach (T element in sequence)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                foreach (T insertedElement in nodesToInsert(last!, element))
                    yield return insertedElement;
            }
            last = element;
            yield return element;
        }
    }
    /// <summary>
    /// Returns a new sequence with the the specified elements inserted at the specified index.
    /// </summary>
    public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> sequence, int index, IEnumerable<T> items)
    {
        if (sequence is null) throw new ArgumentNullException(nameof(sequence));
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (sequence.TryGetNonEnumeratedCount(out int sequenceCount) && index > sequenceCount) throw new ArgumentOutOfRangeException(nameof(index));

        int i = 0;
        foreach (T element in sequence)
        {
            if (i == index)
            {
                foreach (T insertion in items)
                    yield return insertion;
            }
            yield return element;
        }
    }

    /// <summary> Prepends the specified elements to the specified sequence. </summary>
    /// <param name="sequence"> The sequence to be prepended. </param>
    /// <param name="elements"> The elements to prepend to the sequence. </param>
    /// <returns> the sequence containing first the specified elements and then the original sequence. </returns>
    public static IEnumerable<T> Prepend<T>(this IEnumerable<T> sequence, params T[] elements)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(elements != null);

        foreach (T element in elements)
            yield return element;

        foreach (T element in sequence)
            yield return element;
    }

    /// <summary> Modifies the first element in the specified sequence and returns the resulting sequence. </summary>
    /// <param name="sequence"> The sequence to alter. </param>
    /// <param name="getNewFirstElement"> A function returning the new first element, when given the specified first element. </param>
    public static IEnumerable<T> ModifyFirst<T>(this IEnumerable<T> sequence, Func<T, T> getNewFirstElement)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(getNewFirstElement != null);

        using (var enumerator = sequence.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException("sequence empty");

            yield return getNewFirstElement(enumerator.Current);

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
    /// <summary> Modifies the last element in the specified sequence and returns the resulting sequence. </summary>
    /// <param name="sequence"> The sequence to alter. </param>
    /// <param name="getNewLastElement"> A function returning the new last element, when given the specified last element. </param>
    public static IEnumerable<T> ModifyLast<T>(this IEnumerable<T> sequence, Func<T, T> getNewLastElement)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(getNewLastElement != null);

        using (var enumerator = sequence.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException("sequence empty");

            T previousElement = enumerator.Current;
            while (enumerator.MoveNext())
            {
                yield return previousElement;
                previousElement = enumerator.Current;
            }
            yield return getNewLastElement(previousElement);
        }
    }
    /// <summary>
    /// Evaluates the specified enumerable lazily.
    /// </summary>
    public static IEnumerable<T> EvaluateLazily<T>(Func<IEnumerable<T>> getSequence)
    {
        Contract.Requires(getSequence != null);

        var sequence = getSequence();
        Contract.Assume(sequence != null, $"{nameof(getSequence)} is not allowed to return null");

        foreach (T element in sequence)
            yield return element;
    }
    /// <summary> Gets all combinations of n integers in the range [0, count) without allowing doubles. Does not yield permutations, 
    /// i.e. no yielded combination contains the same integers as another yielded combination (otherwise they would be permutations). </summary>
    /// <param name="count"> The exclusive maximum of the range of integers to combine. Must be positive. </param>
    /// <param name="n"> The number of integers to combine. Must be positive and smaller than or equal to <code>count</code>. </param>
    /// <returns> If count == n, one combination is yielded. </returns>
    public static IEnumerable<int[]> AllCombinationsInRange(int count, int n)
    {
        Contract.Requires(count > 0);
        Contract.Requires(n > 0);
        Contract.Requires(n <= count);

        var current = new int[n];
        for (int i = 0; i < n; i++)
            current[i] = i;

        int indexInCurrent = n - 1;
        while (indexInCurrent >= 0)
        {
            yield return (int[])current.Clone();

            for (indexInCurrent = n - 1; indexInCurrent >= 0; indexInCurrent--)
            {
                if (current[indexInCurrent] < count - n + indexInCurrent)
                {
                    current[indexInCurrent]++;
                    for (indexInCurrent++; indexInCurrent < n; indexInCurrent++)
                    {
                        current[indexInCurrent] = current[indexInCurrent - 1] + 1;
                    }
                    break;
                }
            }
        }
    }
    /// <summary> Returns all integers from start to start + count with the specified step. </summary>
    public static IEnumerable<int> Range(int start, int count, int step)
    {
        for (int i = start; i < start + count; i++)
        {
            yield return start;
        }
    }

    /// <summary> Returns all distinct arrays where the nth integer varies from 0 to counts[n] (exclusive). </summary>
    public static IEnumerable<int[]> Range(IList<int> counts)
    {
        return Range(counts, new int[counts.Count]);
    }
    /// <summary> Returns all distinct arrays where the nth integer varies from start[n] (inclusive) to counts[n] (exclusive). </summary>
    public static IEnumerable<int[]> Range(IList<int> counts, IList<int> start)
    {
        if (counts == null) throw new ArgumentNullException("counts");
        if (start == null) throw new ArgumentNullException("start");
        if (counts.Count != start.Count) throw new ArgumentException("lengths.Length != start.Length");
        if (!counts.All(i => i >= 0)) throw new ArgumentException("negative length");

        int[] lengths = new int[counts.Count];
        for (int i = 0; i < lengths.Length; i++)
            lengths[i] = counts[i] - start[i];

#if DEBUG
        long total = 1;
        for (int i = 0; i < lengths.Length; i++)
            total *= lengths[i];
#endif

        // ReSharper disable ForControlVariableIsNeverModified
        for (int[] current = new int[lengths.Length]; current[current.Length - 1] != lengths[current.Length - 1]; Increment(current, lengths))
        {
            int[] result = new int[current.Length];
            for (int i = 0; i < current.Length; i++)
                result[i] = start[i] + current[i];
            yield return result;
#if DEBUG
            total--;
        }
        if (total != 0) throw new Exception();
#else
			}
#endif
    }
    /// <summary> Increments the current array by one, where each value is limited by the length at the same index. </summary>
    private static void Increment(int[] current, int[] lengths)
    {
        for (int indexInCurrent = 0; indexInCurrent < current.Length; indexInCurrent++)
        {
            if (current[indexInCurrent] < lengths[indexInCurrent] - 1)
            {
                current[indexInCurrent]++;
                return;
            }
            else
            {
                current[indexInCurrent] = 0;
            }
        }
        //to terminate the algorithm
        current[current.Length - 1] = lengths[lengths.Length - 1];
    }
    /// <summary> Removes all elements in the specified list after the specified index. </summary>
    /// <typeparam name="T"> The type of the elements in the list. </typeparam>
    /// <param name="list"> The list in which to remove elements. </param>
    /// <param name="startIndex"> The index from which all subsequent elements will be removed from the list. Must be smaller than the number of elements in the list (otherwise consider using "Truncate". </param>
    public static void RemoveRange<T>(this List<T> list, int startIndex)
    {
        Contract.Requires(list != null);
        Contract.Requires(startIndex >= 0);
        Contract.Requires(startIndex < list.Count);

        list.RemoveRange(startIndex, list.Count - startIndex);
    }
    /// <summary> Returns the single element in the specified sequence that matches the specified predicate if there is exactly one; otherwise <code>default(<typeparam name="T">T</typeparam>)</code>.
    /// (Differs from <code>Enumerable.SingleOrDefault</code> in that this method doesn't throw when the argument contains multiple elements). </summary>
    public static T? SingletonOrDefault<T>(this IEnumerable<T> sequence, Func<T, bool>? predicate = null)
    {
        Contract.Requires(sequence != null);

        predicate = predicate ?? (_ => true);

        using (var enumerator = sequence.Where(predicate).GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                T result = enumerator.Current;
                if (!enumerator.MoveNext()) // if sequence doesn't have second element
                {
                    return result;
                }
            }
        }
        return default(T);
    }
    /// <summary> Removes all elements in the specified list from the specified index (inclusive). </summary>
    /// <typeparam name="T"> The type of the elements in the list. </typeparam>
    /// <param name="list"> The list in which to remove elements. </param>
    /// <param name="startIndex"> The index from which all subsequent elements (if any) will be removed from the list. If a number equal to or larger than list.Count is specified, nothing happens. </param>
    public static void Truncate<T>(this List<T> list, int startIndex)
    {
        Contract.Requires(list != null);
        Contract.Requires(startIndex >= 0);

        if (list.Count > startIndex)
            list.RemoveRange(startIndex, list.Count - startIndex);
    }

    /// <summary> The count that can be the last element in the argument <code>counts</code> to <code>SplitByCounts</code> indicating that all remaining elements must be yielded. </summary>
    private const int appendedRemainingCount = Int32.MaxValue;
    /// <summary> Returns all elements in the enumerable in lists where each list contains elements from one index to the next as specified by the list of indices. 
    /// Returns the splitted parts in the list form to avoid laziness issues. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The enumerable whose elements to split per index. </param>
    /// <param name="indices"> The indices to split the enumerable by. Cannot be null. Elements (if any) must be ascending and nonnegative. </param>
    public static IEnumerable<List<T>> SplitAtIndices<T>(this IEnumerable<T> sequence, ISortedEnumerable<int> indices)
    {
        /*IEnumerable<int> counts = indices.Windowed2(0)
											 .Select(tuple => tuple.Item2 - tuple.Item1);*/
        int cumulativeIndex = 0;
        IEnumerable<int> counts = indices.Select(i =>
        {
            int result = i - cumulativeIndex;
            Contract.Assert(result >= 0, "the specified indices aren't sorted");
            cumulativeIndex = i;
            return result;
        });
        return SplitByCounts(sequence, counts.Concat(appendedRemainingCount));
    }
    /// <summary> Splits the elements into multiple sequences, where the number of element per enumerables is specified in a sequence of integers. </summary>
    /// <typeparam name="T"> The type of the elements to split. </typeparam>
    /// <param name="sequence"> The sequence to split into multiple sequences. </param>
    /// <param name="counts"> The number of elements per yielded sequence. The sum must be equal to the number of elements in the specified sequence, 
    /// unless the last element in <code>counts</code> is <code>int.MaxValue</code>, which denotes that the remaining elements in the specified sequence 
    /// will be in an extra sequence yielded at the end. </param>
    public static IEnumerable<List<T>> SplitByCounts<T>(this IEnumerable<T> sequence, IEnumerable<int> counts)
    {
        // ReSharper disable PossibleMultipleEnumeration
        Contract.Requires(sequence != null);
        Contract.Requires(counts != null);

        EnsureSingleEnumerationDEBUG(ref sequence);
        EnsureSingleEnumerationDEBUG(ref counts);
        //Contract.Assert(sequence.Count() == counts.Sum());

        using (IEnumerator<T> elements = sequence.GetEnumerator())
        {
            foreach (int count in counts)
            {
                // ReSharper restore PossibleMultipleEnumeration
                int i = 0;
                int capacity = count == appendedRemainingCount ? 4 : (count + 1);
                var result = new List<T>(capacity);
                while (i != count)
                {
                    //this loop just fills the new result up until the next index
                    if (!elements.MoveNext())
                    {
                        //running out of elements can only happen during the last list, for which the next index has a special value
                        if (count == appendedRemainingCount)
                        {
                            yield return result;
                            yield break;
                        }
                        else
                        {
                            elements.Dispose(); //not sure whether this is necessary; iterators and exceptions combine to a strange beast
                            throw new ArgumentOutOfRangeException(String.Format("Index {0} is out of range. ", count));
                        }
                    }
                    result.Add(elements.Current);
                    i++;
                }
                yield return result;
            }
            if (elements.MoveNext())
                throw new ArgumentException("Counts should sum up to the number of elements in the sequence or int.MaxValue should be the last count");
        }
    }
    /// <summary> Splits the enumerable at locations specified by a function into multiple enumerables. 
    /// The resulting enumerables must be enumerated over entirely before requesting the next enumerable. An exception is thrown otherwise. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable to split. Cannot be null. </param>
    /// <param name="insertInNew"> The function which takes an element and its index in the specified enumerable and returns whether a new enumerable should be started here. </param>
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, Func<T, int, bool> insertInNew)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
        if (insertInNew == null) throw new ArgumentNullException(nameof(insertInNew));

        using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                //no elements in the enumerable should result in no enumerables in the resulting enumerable
                yield break;
            }
            int i = 0;
            WrappedEnumerator<T>? lastResult = null;
            do
            {
                lastResult = new WrappedEnumerator<T>(enumerator, element => insertInNew(element, i++));
                yield return lastResult;

                if (!lastResult.WrapperExhausted)
                    throw new InvalidOperationException("A new enumerable was requested before the last one was fully iterated over");
            }
            while (!lastResult.WrappedEnumeratorExhausted);
        }
    }

    /// <summary> Takes the elements in a certain interval from the specified sequence. </summary>
    public static IEnumerable<T> Take<T>(this IEnumerable<T> sequence, Interval interval)
    {
        Contract.Requires(sequence != null);
        interval.AssertInvariants();

        var start = interval.Start + (interval.StartInclusive ? 0 : 1);
        int length = interval.Length + 1 - (interval.StartInclusive ? 0 : 1) - (interval.EndInclusive ? 0 : 1);
#if DEBUG
        int debugCounter = 0;
        foreach (T element in sequence.Skip(start).Take(length))
        {
            yield return element;
            debugCounter++;
        }
        Contract.Assert<IndexOutOfRangeException>(debugCounter == length);
#else
			return sequence.Skip(start).Take(length);
#endif
    }
    /// <summary> Takes the elements in a certain interval from the specified sequence. </summary>
    public static IEnumerable<T> Take<T>(this IEnumerable<T> sequence, IEnumerable<int> indices)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(indices != null);
        var sortedIndices = indices.ToSortedList();
        Contract.Requires(sortedIndices.Count == 0 || sortedIndices[0] >= 0, $"Negative index ${sortedIndices[0]} specified");

        int sequenceIndex = -1;
        using (var enumerator = sequence.GetEnumerator())
        {
            foreach (var takeIndex in sortedIndices)
            {
                while (sequenceIndex != takeIndex)
                {
                    if (!enumerator.MoveNext())
                        throw new ArgumentOutOfRangeException($"Index ${takeIndex}. Enumerable length: ${sequenceIndex}");
                    sequenceIndex++;
                }
                yield return enumerator.Current;
            }
        }
    }
    /// <summary> Returns the elements while they match the given predicate. </summary>
    /// <param name="predicate"> The function determining whether an element is eligible for returning. </param>
    /// <param name="endOfSequenceReached"> A boolean indicating whether all elements in the original sequence matched the predicate and were returned. </param>
    public static List<T> TakeWhile<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, out bool endOfSequenceReached)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        endOfSequenceReached = true;

        var result = new List<T>();
        foreach (var element in sequence)
        {
            if (!predicate(element))
            {
                endOfSequenceReached = false;
                break;
            }
            result.Add(element);
        }
        return result;
    }
    /// <summary>
    /// Gets all elements in the first sequence that match the first predicate, then still returns all the elements in the sequence that match the second predicate.
    /// </summary>
    public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> sequence, Func<T, bool> firstPredicate, Func<T, bool> secondPredicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(firstPredicate != null);
        Contract.Requires(secondPredicate != null);

        return implementation();
        IEnumerable<T> implementation()
        {
            bool firstPredicateOffended = false;
            foreach (T element in sequence)
            {
                if (!firstPredicateOffended)
                {
                    firstPredicateOffended = !firstPredicate(element);
                    if (!firstPredicateOffended)
                    {
                        yield return element;
                        continue;
                    }
                }

                if (secondPredicate(element))
                {
                    yield return element;
                }
                else
                {
                    yield break;
                }
            }
        }
    }
    /// <summary> Applies the specified selector to the specified sequence while it meets the specified predicate. Returns null otherwise. </summary>
    public static List<TResult>? SelectAllOrNull<T, TResult>(this IEnumerable<T> sequence, Func<T, TResult> selector, Func<T, TResult, bool> predicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(selector != null);
        Contract.Requires(predicate != null);

        var result = new List<TResult>();
        foreach (var element in sequence)
        {
            var selected = selector(element);
            if (!predicate(element, selected))
            {
                return null;
            }
            result.Add(selected);
        }
        return result;
    }
    public static IEnumerable<T> TakeExceptLast<T>(this IEnumerable<T> sequence)
    {
        using (var enumerator = sequence.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                yield break;

            T previous = enumerator.Current;
            while (enumerator.MoveNext())
            {
                yield return previous;
                previous = enumerator.Current;
            }
        }
    }
    /// <summary> Puts all elements of the specified sequence in an array, using the <code>List&lt;T&gt;</code> constructor with the specified initial capacity. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable to create an array out of. </param>
    /// <param name="initialCapacity"> The initial capacity to give the list from which the array is eventually created. </param>
    public static List<T> ToList<T>(this IEnumerable<T> enumerable, int initialCapacity)
    {
        List<T> result = new List<T>(initialCapacity);
        result.AddRange(enumerable);
        return result;
    }
    /// <summary> Puts all elements of the specified sequence in an array of specific length. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The enumerable to create an array out of. </param>
    /// <param name="length"> The length of the to be returned array. Must be equal to or larger than the number of elements in the specified enumerable. </param>
    [DebuggerHidden]
    public static T[] ToArray<T>(this IEnumerable<T> enumerable, int length)
    {
        T[] result = new T[length];
        int i = 0;
        foreach (T element in enumerable)
            result[i++] = element;
        return result;
    }
    /// <summary> Gets whether all elements in the specified sequence are unique. The empty sequence is considered unique. </summary>
    /// <param name="equalityComparer"> The comparer to use to determine uniqueness. Specify null to use the default equality comparer. </param>
    /// <param name="sequence"> The sequence to determine the uniqueness of each element of. </param>
    public static bool AreUnique<T>(this IEnumerable<T> sequence, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence != null);

        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        HashSet<T> uniqueElements = new HashSet<T>(equalityComparer);
        foreach (T element in sequence)
            if (!uniqueElements.Add(element))
                return false;

        return true;
    }
    [DebuggerHidden]
    public static bool AreUnique<T>(this IEnumerable<T> sequence, Func<T?, T?, bool> equalityComparer)
    {
        return sequence.AreUnique(equalityComparer.ToEqualityComparer());
    }

    /// <summary> Gets all unique elements in the specified sequence. Redirects to the default name in the BLC: Distinct() </summary>
    /// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
    /// <param name="sequence"> The sequence to get all unique elements of. </param>
    /// <param name="equalityComparer"> The comparer determining equality between elements in the sequence. Specify null to use the default comparer. </param>
    [DebuggerHidden]
    public static IEnumerable<T?> Unique<T>(this IEnumerable<T> sequence, Func<T?, T?, bool>? equalityComparer = null)
    {
        return sequence.Distinct(equalityComparer);
    }
    /// <summary> Gets all unique elements in the specified sequence. </summary>
    /// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
    /// <param name="sequence"> The sequence to get all unique elements of. </param>
    /// <param name="equalityComparer"> The comparer determining equality between elements in the sequence. Specify null to use the default comparer. </param>
    [DebuggerHidden]
    public static IEnumerable<T?> Distinct<T>(this IEnumerable<T> sequence, Func<T?, T?, bool>? equalityComparer)
    {
        Contract.Requires(sequence != null);

        return sequence.Distinct(equalityComparer?.ToEqualityComparer() ?? EqualityComparer<T>.Default);
    }

    /// <summary> Gets whether all elements in the specified sequence are the same element. For the empty sequence, true is returned. </summary>
    /// <param name="equalityComparer"> The comparer to use to determine uniqueness. Specify null to use the default equality comparer. </param>
    /// <param name="sequence"> The sequence to determine the equality of each element of. If empty, true is returned. </param>
    public static bool AreEqual<T>(this IEnumerable<T> sequence, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence != null);
        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        bool first = true;
        T? firstElement = default(T);
        foreach (T element in sequence)
        {
            if (first)
            {
                first = false;
                firstElement = element;
            }
            else if (!equalityComparer.Equals(element, firstElement))
            {
                return false;
            }
        }

        return true;
    }
    /// <summary> Gets whether all elements in the specified sequence are the same element by reference. For the empty sequence, true is returned. </summary>
    /// <param name="sequence"> The sequence to determine the equality of each element of. If empty, true is returned. </param>
    public static bool AreEqualByRef<T>(this IEnumerable<T> sequence) where T : class
    {
        return sequence.AreEqual(ReferenceEqualityComparer.Instance);
    }


    /// <summary> Gets whether the specified sequence is ordered according to the given comparer. The empty sequence is considered ordered. </summary>
    /// <param name="sequence"> The sequence to check for being ordered. </param>
    /// <param name="comparer"> The comparer determining the order to check for. Specify null to use the defautl comparer. </param>
    public static bool IsOrdered<T>(this IEnumerable<T> sequence, IComparer<T>? comparer = null)
    {
        Contract.Requires(sequence != null);
        comparer = comparer.OrDefault();

        bool first = true;
        T? previous = default(T);
        foreach (T element in sequence)
        {
            if (first)
                first = false;
            else if (comparer.Compare(previous, element) > 0)
                return false;
            previous = element;
        }
        return true;
    }
    /// <summary> Maps an array into another array of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting array.</typeparam>
    /// <param name="array"> The array to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    [DebuggerHidden]
    public static TResult[] Map<T, TResult>(this T[] array, Func<T, TResult> resultSelector)
    {
        Contract.Requires(array != null);
        Contract.Requires(resultSelector != null);

        TResult[] result = new TResult[array.Length];
        for (int i = 0; i < array.Length; i++)
            result[i] = resultSelector(array[i]);

        Contract.Ensures(result != null);
        Contract.Ensures(result.Length == array.Length);
        return result;
    }
    /// <summary>
    /// Casts the entire array from <typeparamref name="T"/> to <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="array">The source array.</param>
    public static TResult[] CastAll<T, TResult>(this T[] array) where TResult : class
    {
        return Array.ConvertAll<T, TResult>(array, element => (TResult)(object)element!);
    }

    /// <summary> Maps an array into another array of the same size using a specified mapping function depending also on the elements index. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting array.</typeparam>
    /// <param name="array"> The array to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    public static TResult[] Map<T, TResult>(this T[] array, Func<T, int, TResult> resultSelector)
    {
        Contract.Requires(array != null);
        Contract.Requires(resultSelector != null);

        var result = new TResult[array.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = resultSelector(array[i], i);

        Contract.Ensures(result != null);
        Contract.Ensures(result.Length == array.Length);
        return result;
    }
    /// <summary> Maps a list into another list of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    public static List<TResult> Map<T, TResult>(this List<T> list, Func<T, TResult> resultSelector)
    {
        return ((IList<T>)list).Map(resultSelector);
    }
    /// <summary> Maps an list into another list of the same size using a specified mapping function depending also on the elements index. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    public static List<TResult> Map<T, TResult>(this List<T> list, Func<T, int, TResult> resultSelector)
    {
        return ((IList<T>)list).Map(resultSelector);
    }

    /// <summary> Maps a list into another list of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    public static List<TResult> Map<T, TResult>(this IList<T> list, Func<T, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        var result = new List<TResult>(list.Count);
        foreach (T t in list)
            result.Add(resultSelector(t));

        Contract.Ensures(result != null);
        Contract.Ensures(result.Count == list.Count);
        return result;
    }
    /// <summary> Maps an list into another list of the same size using a specified mapping function depending also on the elements index. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    public static List<TResult> Map<T, TResult>(this IList<T> list, Func<T, int, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        var result = new List<TResult>(list.Count);
        for (int i = 0; i < list.Count; i++)
            result.Add(resultSelector(list[i], i));

        Contract.Ensures(result != null);
        Contract.Ensures(result.Count == list.Count);
        return result;
    }
    /// <summary> Maps the specified list lazily to another list, meaning that each mapped element is only computed (and cached) on demand. </summary>
    /// <typeparam name="TSource"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map to. </typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function applying the map to each element. </param>
    public static LazyArray<TSource, TResult> MapCached<TSource, TResult>(this IList<TSource> list, Func<TSource, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        throw new NotImplementedException(); // return new LazyArray<TSource, TResult>(list, resultSelector);
    }
    /// <summary> Maps the specified list lazily: each mapped element is computed on demand (and is not cached). </summary>
    /// <typeparam name="TSource"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map to. </typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function applying the map to each element. </param>
    public static IReadOnlyList<TResult> MapLazily<TSource, TResult>(this IList<TSource> list, Func<TSource, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        Func<int, TResult> selector = index =>
        {
            var element = list[index];
            return resultSelector(element);
        };
        return new LazyReadOnlyArray<TResult>(selector, list.Count);
    }
    /// <summary> Maps the specified list lazily: each mapped element is computed on demand (and is not cached). </summary>
    /// <typeparam name="TSource"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map to. </typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function applying the map to each element. </param>
    [DebuggerHidden]
    public static IReadOnlyList<TResult> MapLazily<TSource, TResult>(this IReadOnlyList<TSource> list, Func<TSource, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        Func<int, TResult> selector = index =>
        {
            var element = list[index];
            return resultSelector(element);
        };
        return new LazyReadOnlyArray<TResult>(selector, list.Count);
    }
    /// <summary> Maps the specified list lazily: each mapped element is computed on demand (and is not cached). </summary>
    /// <typeparam name="TSource"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map to. </typeparam>
    /// <param name="list"> The list to map. </param>
    /// <param name="resultSelector"> The function applying the map to each element. </param>
    [DebuggerHidden]
    public static IReadOnlyList<TResult> MapLazily<TSource, TResult>(this IReadOnlyList<TSource> list, Func<TSource, int, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        Func<int, TResult> selector = index =>
        {
            var element = list[index];
            return resultSelector(element, index);
        };
        return new LazyReadOnlyArray<TResult>(selector, list.Count);
    }
    [DebuggerHidden]
    public static IReadOnlyList<TResult> CastLazily<TSource, TResult>(this IReadOnlyList<TSource> list)
    {
        return list.MapLazily(element => (TResult)(object)element!);
    }
    /// <summary> Maps a readonly collection into another of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The collection to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    [DebuggerHidden]
    public static ReadOnlyCollection<TResult> Map<T, TResult>(this ReadOnlyCollection<T> list, Func<T, TResult> resultSelector)
    {
        Contract.Requires(list != null);
        Contract.Requires(resultSelector != null);

        return ((IReadOnlyList<T>)list).Map(resultSelector);
    }
    /// <summary> Maps the specified collection to another by identity conversions of the elements. </summary>
    [DebuggerHidden]
    public static ReadOnlyCollection<TResult> Map<T, TResult>(this ReadOnlyCollection<T> list)
    {
        return list.Map((T t) => (TResult)(object)t!);
    }
    /// <summary> Maps a readonly collection into another of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The collection to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    [DebuggerHidden]
    public static ReadOnlyCollection<TResult> Map<T, TResult>(this IReadOnlyList<T> list, Func<T, TResult> resultSelector)
    {
        var result = new TResult[list.Count];
        int i = 0;
        foreach (T t in list)
            result[i++] = resultSelector(t);
        return new ReadOnlyCollection<TResult>(result);
    }
    /// <summary> Maps a readonly collection into another of the same size using a specified mapping function. </summary>
    /// <typeparam name="T"> The type of the elements to map into the type <code>TResult</code>. </typeparam>
    /// <typeparam name="TResult"> The type of the elements in the resulting collection.</typeparam>
    /// <param name="list"> The collection to map. </param>
    /// <param name="resultSelector"> The function that maps a given element into a resulting element. </param>
    [DebuggerHidden]
    public static ReadOnlyCollection<TResult> Map<T, TResult>(this IReadOnlyList<T> list, Func<T, int, TResult> resultSelector)
    {
        var result = new TResult[list.Count];
        int i = 0;
        foreach (T t in list)
        {
            result[i] = resultSelector(t, i);
            i++;
        }
        return new ReadOnlyCollection<TResult>(result);
    }

    /// <summary> Returns whether the sequence contains the specified items in order of their occurrence. 
    /// Any number of elements may be in between though. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The sequence to check whether it has all items in their order. </param>
    /// <param name="items"> The items to look for in the sequence. </param>
    /// <param name="equalityComparer"> The equality comparer. When null, the default is used. </param>
    /// <returns> If items is empty, true is returned. </returns>
    public static bool ContainsOrdered<T>(this IEnumerable<T> enumerable, IEnumerable<T> items, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(enumerable != null);
        Contract.Requires(items != null);

        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        using (var enumerator = enumerable.GetEnumerator())
        {
            foreach (var item in items) //TODO: enumerate items only once
            {
                bool found = false;
                while (enumerator.MoveNext())
                    if (equalityComparer.Equals(enumerator.Current, item))
                    {
                        found = true;
                        break;
                    }
                if (!found)
                {
                    //enumerable exhausted with still items to go
                    return false;
                }
            }
            return true;
        }
    }
    /// <summary> Returns whether the sequence contains the specified items successively. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The sequence to check whether it has all specified items successively. </param>
    /// <param name="items"> The items to look for in the sequence. If empty, true is returned. </param>
    /// <param name="equalityComparer"> The equality comparer. When null, the default is used. </param>
    /// <returns> If items is empty, true is returned. </returns>
    public static bool Contains<T>(this IEnumerable<T> enumerable, IEnumerable<T> items, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(enumerable != null);
        Contract.Requires(items != null);

        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        var sequence = items.ToList();
        if (sequence.Count == 0)
            return true;

        return enumerable.IndexOf(sequence, equalityComparer.Equals) != -1;
    }

    /// <summary> Gets the first element in the specified sequence that matches the specified predicate, which uses the index of the element in the sequence. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence whose first match is to be returned. </param>
    /// <param name="predicate"> A delegate determining whether the element and its index in the sequence matches a condition. </param>
    /// <returns> Returns the first match in the sequence or <code>default(T)</code> if there is no match. </returns>
    public static T? FirstOrDefault<T>(this IEnumerable<T> sequence, Func<T, int, bool> predicate)
    {
        int i = 0;
        foreach (var element in sequence)
            if (predicate(element, i++))
                return element;
        return default(T);
    }

    /// <summary> Returns the index of the first element matching the specified predicate. Returns -1 if no elements match it. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The enumerator yielding elements to check for a match. </param>
    /// <param name="predicate"> The function determining whether an element matches. </param>
    public static int IndexOf<T>(this IEnumerator<T> sequence, Func<T, bool> predicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(predicate != null);

        int i = 0;
        while (sequence.MoveNext())
        {
            if (predicate(sequence.Current))
                return i;
            i++;
        }

        return -1;
    }
    /// <summary> Skips the specified number of elements, and returns whether it did so. </summary>
    /// <param name="sequence"> The enumerator that is to skip elements. </param>
    /// <param name="count"> The number of elements to skip. </param>
    /// <returns> whether the specified number of elements were skipped. This equals the last returns value of enumerator.MoveNext(), or true if no such call was made. </returns>
    public static bool Skip<T>(this IEnumerator<T> sequence, int count)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(count >= 0);

        for (int skipped = 0; skipped < count; skipped++)
        {
            if (!sequence.MoveNext())
                return false;
        }
        return true;
    }

    /// <summary> Returns the index of the first element matching the specified predicate. Returns -1 if no elements match it. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The elements to check for a match. </param>
    /// <param name="predicate"> The function determining whether an element matches. </param>
    public static int IndexOf<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
    {
        return sequence.IndexOf((element, i) => predicate(element));
    }
    /// <summary> Returns the index of the first element matching the specified predicate. Returns -1 if no elements match it. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The elements to check for a match. </param>
    /// <param name="predicate"> The function determining whether an element matches. </param>
    public static int IndexOf<T>(this IEnumerable<T> sequence, Func<T, int, bool> predicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(predicate != null);

        int i = 0;
        foreach (T element in sequence)
        {
            if (predicate(element, i))
                return i;
            i++;
        }

        return -1;
    }
    [DebuggerHidden]
    public static int IndexOf<T>(this IEnumerable<T> sequence, T item)
    {
        return sequence.IndexOf(item, null);
    }
    /// <summary> Returns the index of the first element matching the specified item using the specified equality comparer. Returns -1 if no elements match it. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The elements to check for a match. </param>
    /// <param name="item"> The item to search for in the specified sequence. </param>
    /// <param name="equalityComparer"> The object determining whether an element matches the specified item. </param>
    [DebuggerHidden]
    public static int IndexOf<T>(this IEnumerable<T> sequence, T item, IEqualityComparer<T>? equalityComparer)
    {
        Contract.Requires(sequence != null);

        equalityComparer ??= EqualityComparer<T>.Default;

        int i = 0;
        foreach (T element in sequence)
            if (equalityComparer.Equals(item, element))
                return i;
            else
                i++;

        return -1;
    }
    /// <summary> Returns the elements of the specified list in the specified range. </summary>
    /// <typeparam name="T"> The type of the elements in the list. </typeparam>
    /// <param name="list"> The list to return the elements of. </param>
    /// <param name="start"> The index of the first element to return. </param>
    /// <param name="count"> The number of elements to return. </param>
    public static IEnumerable<T> Range<T>(this IList<T> list, int start, int count)
    {
        Contract.Requires(list != null);
        Contract.Requires(0 <= start);
        Contract.Requires(count >= 0);
        Contract.Requires(start + count <= list.Count);

        for (int i = 0; i < count; i++)
            yield return list[start + i];
    }
    /// <summary> Applies a function to each element of a sequence, threading an accumulator argument through the computation. </summary>
    public static IEnumerable<TResult> Scan<T, TResult>(this IEnumerable<T> enumerable, Func<TResult, T, TResult> f, TResult seed)
    {
        Contract.Requires(enumerable != null);
        Contract.Requires(f != null);

        var current = seed;
        foreach (T element in enumerable)
        {
            current = f(current, element);
            yield return current;
        }
    }

    /// <summary> Yields all elements in the specified item tupled with the previous item. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The elements to pair. </param>
    /// <returns> If the enumerable contains fewer than two elements, an empty enumerable is returned. </returns>
    [DebuggerHidden]
    public static IEnumerable<(T First, T Second)> Windowed2<T>(this IEnumerable<T> enumerable)
    {
        Contract.Requires(enumerable != null);

        T? previous = default(T);
        bool first = true;
        foreach (T element in enumerable)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                yield return (previous!, element);
            }
            previous = element;
        }
    }
    /// <summary> Yields all elements in the specified item tupled with the previous item, where an item previous to the first element can be specified. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The elements to pair. </param>
    /// <param name="zeroth"> The item that is considered to be previous to the first element in the specified sequence. </param>
    /// <returns> an enumerable that has the exact same number of elements as the input sequence. </returns>
    [DebuggerHidden]
    public static IEnumerable<(T First, T Second)> Windowed2<T>(this IEnumerable<T> enumerable, T zeroth)
    {
        Contract.Requires(enumerable != null);

        return enumerable.Prepend(zeroth)
                         .Windowed2();
    }
    /// <summary> Yields all elements in the specified item tupled with the previous item and the index of the tuple (equal to the index in enumerable of the first element), so this index ranges from [0, enumerable.Count() - 1) </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="enumerable"> The elements to pair. </param>
    /// <returns> If the enumerable contains fewer than two elements, an empty enumerable is returned. </returns>
    public static IEnumerable<(T First, T Second, int Index)> IndexedWindowed2<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Windowed2().Select((t, i) => (t.First, t.Second, i));
    }
    /// <summary>
    /// Returns a new sequence with the specified element appended.
    /// </summary>
    [DebuggerHidden]
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, T item)
    {
        Contract.Requires(sequence != null);

        foreach (T element in sequence)
            yield return element;
        yield return item;
    }
    [DebuggerHidden]
    public static bool All(this IEnumerable<bool> sequence)
    {
        return sequence.All(x => x);
    }
    public static bool Any(this IEnumerable<bool> sequence)
    {
        return sequence.Any(x => x);
    }
    /// <summary> Returns whether any of the specified elements matches the specified predicate which an element and its index in the sequence. </summary>
    public static bool Any<T>(this IEnumerable<T> sequence, Func<T, int, bool> predicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(predicate != null);

        return sequence.Where(predicate).Any();
    }
    /// <summary> Outputs the specified sequence to list, or null if a null element was encountered. </summary>
    /// <param name="sequence"> The sequence to output to list. </param>
    public static List<T>? ToListNullTerminated<T>(this IEnumerable<T> sequence) where T : class
    {
        Contract.Requires(sequence != null);

        //the case sequence is IList<T> does not provide an optimization

        var result = new List<T>();
        foreach (var element in sequence)
        {
            if (ReferenceEquals(null, element))
                return null;
            result.Add(element);
        }
        return result;
    }
    [DebuggerHidden]
    /// <summary> Caches the specified sorted sequence in a sorted list. </summary>
    public static SortedList<T> ToSortedList<T>(this ISortedEnumerable<T> sequence)
    {
        return new SortedList<T>(sequence.ToList(), sequence.Comparer);
    }
    /// <summary> Sorts the specified sequence and wraps it is a sorted list. </summary>
    /// <param name="sequence"> The sequence to sort and wrap. </param>
    /// <param name="comparer"> The comparer determing the order to sort in. Specify null to use the default comparer. </param>
    [DebuggerHidden]
    public static SortedList<T> ToSortedList<T>(this IEnumerable<T> sequence, Func<T, T, int>? comparer = null)
    {
        Contract.Requires(sequence != null);
        IComparer<T> c = comparer == null ? Comparer<T>.Default : InterfaceWraps.ToComparer(comparer!);

        var list = sequence.ToList();
        list.Sort(c);
        return new SortedList<T>(list, comparer ?? Comparer<T>.Default.Compare);
    }
    /// <summary> Sorts the specified sequence and wraps it is a sorted list. </summary>
    /// <param name="sequence"> The sequence to sort and wrap. </param>
    /// <param name="comparer"> The comparer determing the order to sort in. Specify null to use the default comparer. </param>
    [DebuggerHidden]
    public static SortedList<T> ToSortedList<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
    {
        return sequence.ToSortedList(comparer == null ? default(Func<T, T, int>) : comparer.Compare);
    }
    [DebuggerHidden]
    public static SortedList<T> ToSingletonSortedList<T>(this T element, Func<T, T, int>? comparer = null)
    {
        var list = new T[] { element };
        return ToSortedList(list, comparer);
    }
    /// <summary> Calls ToList on the specified enumerable, and wraps it in a read only collection. </summary>
    /// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
    /// <param name="sequence"> The sequence to wrap in a read only list. </param>
    [DebuggerHidden]
    public static ReadOnlyCollection<T> ToReadOnlyList<T>(this IEnumerable<T> sequence)
    {
        Contract.Requires(sequence != null);

        return new ReadOnlyCollection<T>(sequence.ToList());
    }
    /// <summary> Calls ToList on the specified enumerable, and wraps it in a read only collection. </summary>
    /// <typeparam name="T"> The type of the elements in the sequence. </typeparam>
    /// <param name="sequence"> The sequence to wrap in a read only list. </param>
    /// <param name="initialCapacity"> The initial capacity of the list created. </param>
    [DebuggerHidden]
    public static ReadOnlyCollection<T> ToReadOnlyList<T>(this IEnumerable<T> sequence, int initialCapacity)
    {
        Contract.Requires(sequence != null);

        return new ReadOnlyCollection<T>(sequence.ToList(initialCapacity));
    }
    /// <summary> Maps a list into a read-only list. </summary>
    /// <typeparam name="T"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map onto. </typeparam>
    /// <param name="sequence"> The elements to map. </param>
    /// <param name="resultSelector"> The mapping function. </param>
    public static ReadOnlyCollection<TResult> ToReadOnlyList<T, TResult>(this IList<T> sequence, Func<T, TResult> resultSelector)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(resultSelector != null);

        IList<TResult> underlyingList = new TResult[sequence.Count];
        for (int i = 0; i < sequence.Count; i++)
            underlyingList[i] = resultSelector(sequence[i]);
        return new ReadOnlyCollection<TResult>(underlyingList);
    }
    /// <summary> Maps a list into a read-only list incorporating the element's index. </summary>
    /// <typeparam name="T"> The type of the elements to map. </typeparam>
    /// <typeparam name="TResult"> The type of the elements to map onto. </typeparam>
    /// <param name="sequence"> The elements to map. </param>
    /// <param name="resultSelector"> The mapping function also given the index in the specified sequence of an element. </param>
    public static ReadOnlyCollection<TResult> ToReadOnlyList<T, TResult>(this IList<T> sequence, Func<T, int, TResult> resultSelector)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(resultSelector != null);

        IList<TResult> underlyingList = new TResult[sequence.Count];
        for (int i = 0; i < sequence.Count; i++)
            underlyingList[i] = resultSelector(sequence[i], i);
        return new ReadOnlyCollection<TResult>(underlyingList);
    }

    /// <summary> Has side effects! Lazily outputs all elements to the specified container while yielding the original enumerable. 
    /// Just before a new element is yielded by this method, the element in appended to the cache. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence to enumerate over and cache. </param>
    /// <param name="cache"> The list to append all elements in the sequence to. </param>
    public static IEnumerable<T> CacheLazily<T>(this IEnumerable<T> sequence, IList<T> cache)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(cache != null);

        foreach (T element in sequence)
        {
            cache.Add(element);
            yield return element;
        }
    }
    /// <summary>
    /// Converts the specified sequence to a sequence of the specified element type; When null is specified, an empty sequence is returned.
    /// </summary>
    /// <param name="sequence"> The sequence to convert. </param>
    public static IEnumerable<T> CastOrEmptyForNull<T>(this IEnumerable sequence)
    {
        if (sequence == null)
            return Enumerable.Empty<T>();

        return sequence.Cast<T>();
    }

    public static LazyList<T> ToLazyList<T>(this IEnumerable<T> enumerable)
    {
        Contract.Requires(enumerable != null);

        return new LazyList<T>(enumerable);
    }
    public static LazyList<T> ToLazyList<T>(this IEnumerable<T> enumerable, int initialCapacity)
    {
        Contract.Requires(enumerable != null);
        Contract.Requires(initialCapacity >= 0);

        return new LazyList<T>(enumerable, initialCapacity);
    }
    public static LazyReadOnlyList<T> ToLazyReadOnlyList<T>(this IEnumerable<T> enumerable)
    {
        Contract.Requires(enumerable != null);

        return new LazyReadOnlyList<T>(enumerable);
    }

    public static void Substitute<T>(this IList<T> list, int index, int removeCount, IEnumerable<T> items)
    {
        Contract.Requires(list != null);
        Contract.Requires(items != null);
        Contract.Requires(index >= 0);
        Contract.Requires(removeCount >= 0);
        Contract.Requires(index + removeCount <= list.Count);

        foreach (T item in items)
        {
            if (removeCount == 0)
            {
                list.Insert(index, item);
                index++;
            }
            else
            {
                if (list.Count == index + 1)
                    list.Add(item);
                else
                    list[index] = item;

                index++;
                removeCount--;
            }
        }
        for (int i = removeCount - 1; i >= 0; i--)
        {
            list.RemoveAt(index);
        }
    }
    public static void Substitute<T>(this List<T> list, int index, int removeCount, IEnumerable<T> items)
    {
        Contract.Requires(list != null);
        Contract.Requires(items != null);
        Contract.Requires(index >= 0);
        Contract.Requires(removeCount >= 0);
        Contract.Requires(index + removeCount <= list.Count);

        using (var enumerator = items.GetEnumerator())
        {
            //first overwrite as long as there are elements to remove
            while (removeCount-- > 0)
            {
                if (enumerator.MoveNext())
                {
                    list[index++] = enumerator.Current;
                }
                else
                {
                    //all items inserted: remove up to removeCount
                    list.RemoveRange(index, removeCount + 1); //plus one is to compensate for the -- earlier
                }
            }
            //all items removed: insert all remaining 
            list.InsertRange(index, new WrappedEnumerator<T>(enumerator, _ => false));
        }
    }
    /// <summary> Creates a sequence out of a single specified element. </summary>
    [DebuggerHidden]
    public static IEnumerable<T> ToSingleton<T>(this T element)
    {
        yield return element;
    }
    /// <summary> Creates a new list with the specified element as its single element. </summary>
    [DebuggerHidden]
    public static List<T> ToSingletonList<T>(this T element)
    {
        return new List<T> { element };
    }
    [DebuggerHidden]
    public static ReadOnlyCollection<T> ToSingletonReadOnlyList<T>(this T element)
    {
        return new ReadOnlyCollection<T>(new[] { element });
    }

    /// <summary> Returns whether the specified item is in any of the specified candidates. </summary>
    public static bool IsAnyOf<T>(this T item, IEqualityComparer<T> equalityComparer, params T[] candidates)
    {
        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        return candidates.Any(candidate => equalityComparer.Equals(item, candidate));
    }
    /// <summary> Returns whether the specified item is in any of the specified candidates. </summary>
    public static bool IsAnyOf<T>(this T item, params T[] candidates)
    {
        return item.IsAnyOf(EqualityComparer<T>.Default, candidates);
    }

    /// <summary> Returns whether the specified item is in any of the specified candidates by reference. </summary>
    public static bool IsAnyOfByRef<T>(this T item, params T[] candidates)
    {
        return item.IsAnyOf(ReferenceEqualityComparer.Instance, candidates);
    }
    /// <summary> Returns whether the specified sequence is sorted, optionally according to some comparer. The empty sequence is considered sorted. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence that is checked for being sorted. </param>
    /// <param name="comparer"> A comparer for determining the order checked for. Specifying null will use the default comparer. </param>
    [DebuggerHidden]
    public static bool IsSorted<T>(this IEnumerable<T> sequence, Func<T, T, int>? comparer = null)
    {
        Contract.Requires(sequence != null);

        comparer = comparer.OrDefault();

        T? previous = default;
        bool first = true;
        foreach (T element in sequence)
        {
            if (first)
                first = false;
            else if (comparer(previous!, element) > 0)
                return false;
            previous = element;
        }
        return true;
    }
    [DebuggerHidden]
    public static bool IsSortedDescendingly<T>(this IEnumerable<T> sequence)
    {
        Func<T, T, int> comparer = Comparer<T>.Default.Compare;
        return sequence.IsSorted((a, b) => comparer(b, a));
    }
    /// <summary> Returns whether the specified sequence is sorted, optionally according to some comparer. The empty sequence is considered sorted. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence that is checked for being sorted. </param>
    /// <param name="comparer"> A comparer for determining the order checked for. Specifying null will throw. </param>
    [DebuggerHidden]
    public static bool IsSorted<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
    {
        Contract.Requires(comparer != null);

        return IsSorted(sequence, comparer.Compare);
    }
    /// <summary> Returns whether the specified sequence is sorted by a key, optionally according to some key comparer. The empty sequence is considered sorted. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence that is checked for being sorted. </param>
    /// <param name="comparer"> A comparer for comparing keys to determine the order checked for. Specifying null will throw. </param>
    [DebuggerHidden]
    public static bool IsSortedBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> selectKey, IComparer<TKey> comparer)
    {
        Contract.Requires(sequence != null);

        return sequence.Select(selectKey).IsSorted(comparer);
    }
    /// <summary> Returns whether the specified sequence is sorted by a key, optionally according to some key comparer. The empty sequence is considered sorted. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="sequence"> The sequence that is checked for being sorted. </param>
    /// <param name="comparer"> A comparer for comparing keys to determine the order checked for. Specifying null will throw. </param>
    [DebuggerHidden]
    public static bool IsSortedBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> selectKey, Func<TKey, TKey, int>? comparer = null)
    {
        Contract.Requires(sequence != null);

        return sequence.Select(selectKey).IsSorted(comparer);
    }
    /// <summary> Returns a new readonly collection containing the exact same elements as the specified collection. </summary>
    /// <param name="collection"> The collection to clone. </param>
    public static ReadOnlyCollection<T> MemberwiseClone<T>(this ReadOnlyCollection<T> collection)
    {
        var result = new T[collection.Count];
        collection.CopyTo(result, 0);
        return new ReadOnlyCollection<T>(result);
    }
    /// <summary> Returns the last element of the specified list. </summary>
    /// <typeparam name="T"> The type of the elements of the list. </typeparam>
    /// <param name="list"> The list to return the last item of. </param>
    [DebuggerHidden]
    public static T Last<T>(this IList<T> list)
    {
        Contract.Requires<ArgumentNullException>(list != null);
        Contract.Requires(list.Count > 0, "list empty");

        return list[list.Count - 1];
    }
    /// <summary> Returns the last item matching a predicate, starting at the specified index. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="list"> The list of items to match to the predicate. </param>
    /// <param name="predicate"> The function determining whether an item is a match. </param>
    /// <param name="startIndex"> The index (inclusive) of the first item inspected. </param>
    public static T Last<T>(this IList<T> list, Func<T, bool> predicate, int startIndex)
    {
        for (int i = startIndex; i >= 0; i--)
            if (predicate(list[i]))
                return list[i];
        throw new ArgumentException("No item is the range matches the predicate");
    }
    /// <summary> Returns the last item matching a predicate, starting at the specified index. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="list"> The list of items to match to the predicate. </param>
    /// <param name="predicate"> The function determining whether an item is a match. </param>
    /// <param name="startIndex"> The index (inclusive) of the first item inspected. </param>
    public static T? LastOrDefault<T>(this IList<T> list, Func<T, bool> predicate, int startIndex)
    {
        for (int i = startIndex; i >= 0; i--)
            if (predicate(list[i]))
                return list[i];
        return default(T);
    }
    /// <summary> Returns the index of the last item matching a predicate, starting at the specified index. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="list"> The list of items to match to the predicate. </param>
    /// <param name="predicate"> The function determining whether an item is a match. </param>
    /// <param name="startIndex"> The index (inclusive) of the first item inspected. </param>
    public static int LastIndex<T>(this IList<T> list, Func<T, bool> predicate, int startIndex)
    {
        for (int i = startIndex; i >= 0; i--)
            if (predicate(list[i]))
                return i;
        return -1;
    }
    /// <summary> Gets the second to last item in the specified list. </summary>
    public static T SecondToLast<T>(this IList<T> list)
    {
        Contract.Requires(list != null);
        Contract.Requires(list.Count >= 2);

        return list[list.Count - 2];
    }
    /// <summary> Gets the second to last item in the specified readonly list. </summary>
    public static T SecondToLast<T>(this IReadOnlyList<T> list)
    {
        Contract.Requires(list != null);
        Contract.Requires(list.Count >= 2);

        return list[list.Count - 2];
    }
    public static T[] GetRange<T>(this IList<T> list, int start, int count)
    {
        Contract.Requires(list != null);
        Contract.Requires(start >= 0);
        Contract.Requires(count >= 0);
        Contract.Requires(list.Count >= start + count);
        Contract.Requires(list.Count > start);

        T[] result = new T[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = list[start + i];
        }
        return result;
    }
    /// <summary> Removes the last item in the specified list. </summary>
    /// <param name="list"> The list to remove the lst item of. </param>
    public static void RemoveLast<T>(this IList<T> list)
    {
        Contract.Requires(list != null);

        list.RemoveAt(list.Count - 1);
    }
    public static void RemoveAt<T>(this List<T> list, Index i)
    {
        list.RemoveAt(i.GetOffset(list.Count));
    }

    [DebuggerHidden]
    public static ReadOnlyCollection<T> Subset<T>(this IReadOnlyList<T> t, int start, int end)
    {
        Contract.Requires(end >= start, "end must be larger than or equal to start");
        return new ReadOnlyCollection<T>(Enumerable.Range(start, end - start).Select(i => t[i]).ToArray(end - start)); //can be optimized by creating a dedicated type
    }
    public static IEnumerable<T> Except<T>(this IEnumerable<T> sequence, T item, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence != null);
        equalityComparer ??= EqualityComparer<T>.Default;
        foreach (var element in sequence)
            if (!equalityComparer.Equals(item, element))
                yield return element;
    }
    /// <summary> Replaces the first element in the specified list that matches the specified predicate by the specified element. </summary>
    /// <returns> the index where the new element was placed; or -1. </returns>
    public static int ReplaceIfExists<T>(this IList<T> list, Func<T, bool> predicate, T newItem)
    {
        Contract.Requires(list != null);
        Contract.Requires(predicate != null);

        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list[i] = newItem;
                return i;
            }
        }
        return -1;
    }

    /// <summary> Returns the remainder of the elements presented by the specified enumerator as enumerable, optionally including the current element. </summary>
    /// <param name="includeCurrent"> Specify whether the current element on the enumerator should be yielded by the returned enumerable.
    /// Specify null if there is no current element because the last call to <code>enumerator.MoveNext()</code> returned false. 
    /// This entails that if there is no current element beceause MoveNext() hasn't been called yet, specify false. </param>
    public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator, bool? includeCurrent)
    {
        Contract.Requires(enumerator != null);

        return new EnumeratorWrapper<T>(enumerator, includeCurrent);
    }

    /// <summary> Yields all indices in the specified range except those contained in the specified sequence. </summary>
    public static IEnumerable<TIndex> RangeExcept<TIndex>(this IEnumerable<TIndex> sequence, TIndex start, TIndex end, Func<int, TIndex> ctor) where TIndex : ITinyIndex
    {
        Contract.Requires(sequence != null);
        Contract.Requires(start.Index <= end.Index);
        Contract.Requires(ctor != null);

        return sequence.Select(i => i.Index).RangeExcept(start.Index, end.Index).Select(ctor);
    }
    /// <summary> Yields all integers in the specified range except those contained in the specified sequence. </summary>
    public static IEnumerable<int> RangeExcept(this IEnumerable<int> sequence, int start, int end)
    {
        var set = new HashSet<int>(sequence);
        for (int i = start; i < end; i++)
            if (!set.Contains(i))
                yield return i;
    }

    /// <summary> Returns the specified sequence if all elements match a condition; or null otherwise.  </summary>
    /// <param name="source"> A sequence to apply a predicate to. </param>
    /// <param name="predicate"> A function to test each source element for a condition. </param>
    [DebuggerHidden]
    public static List<T>? AllOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Contract.Requires(source != null);
        Contract.Requires(predicate != null);

        var result = new List<T>();
        foreach (var element in source)
        {
            if (!predicate(element))
            {
                return null;
            }
            result.Add(element);
        }
        return result;
    }

    public static IEnumerable<T> Unfold<T>(this T element, Func<T, Option<T>> selectNext)
    {
        for (Option<T> result = element; result.HasValue; result = selectNext(result.Value))
        {
            yield return result.Value;
        }
    }

    public static IEnumerable<TState> Scan<TState, T>(this IEnumerable<T> sequence, TState initialState, Func<T, TState, TState> apply)
    {
        var state = initialState;
        foreach (T element in sequence)
        {
            state = apply(element, state);
            yield return state;
        }
    }
    public static IEnumerable<TResult> ScanSelect<T, TState, TResult>(this IEnumerable<T> sequence, TState initialState, Func<T, TState, TState> apply, Func<T, TState, TResult> select)
    {
        var state = initialState;
        foreach (T element in sequence)
        {
            state = apply(element, state);
            yield return @select(element, state);
        }
    }
    public static IEnumerable<TResult> Unfold<TState, TResult>(this TState element, Func<TState, (TState, Option<TResult>)> selectNext)
    {
        // if (andSelf)
        //     yield return element;
        (TState state, Option<TResult> result) = selectNext(element);
        while (result.HasValue)
        {
            yield return result.Value;
            (state, result) = selectNext(state);
        }
    }
    public static IEnumerable<TBase> TransitiveVirtualSelect<TBase, TDerived>(this TBase item, Func<TDerived, IEnumerable<TBase>> elementsSelector) where TDerived : TBase
    {
        return item.TransitiveSelect(element =>
        {
            if (element is TDerived)
                return elementsSelector((TDerived)element);
            return EmptyCollection<TBase>.Enumerable;
        });
    }
    //public static IEnumerable<TBase> TransitiveVirtualSelect<TBase, TDerived1, TDerived2>(this TBase item, Func<TDerived2, IEnumerable<TBase>> selectorForDerivedType2) where TDerived1 : TBase where TDerived2 : TBase
    //{
    //	return TransitiveVirtualSelect(item, (TDerived1 _) => Enumerable.Empty<TBase>(), selectorForDerivedType2);
    //}
    //public static IEnumerable<TBase> TransitiveVirtualSelect<TBase, TDerived1, TDerived2>(this TBase item, Func<TDerived1, IEnumerable<TBase>> selectorForDerivedType1) where TDerived1 : TBase where TDerived2 : TBase
    //{
    //	return TransitiveVirtualSelect(item, selectorForDerivedType1, (TDerived2 _) => Enumerable.Empty<TBase>());
    //}
    public static IEnumerable<TBase> TransitiveVirtualSelect<TBase, TDerived1, TDerived2>(this TBase item, Func<TDerived1, IEnumerable<TBase>> selectorForDerivedType1, Func<TDerived2, IEnumerable<TBase>> selectorForDerivedType2) where TDerived1 : TBase where TDerived2 : TBase
    {
        //"virtual" because specifying one function to call per derived type mimicks a vtable
        return item.TransitiveSelect(element =>
        {
            if (element is TDerived1)
                return selectorForDerivedType1((TDerived1)element);
            else if (element is TDerived2)
                return selectorForDerivedType2((TDerived2)element);
            else
                return EmptyCollection<TBase>.Enumerable;
        });
    }

    /// <summary> Selects the specified item, and those selected by a specified function, transitively, and in a depth-first manner. </summary>
    /// <param name="item"> The item to yield and whose elements to select transitively. </param>
    /// <param name="selector"> The function bringing about the transitive relation. </param>
    public static IEnumerable<T> TransitiveSelect<T>(this T item, Func<T, IEnumerable<T>> selector)
    {
        // on naming: this method doesn't select, but traverses. However, it does only traverse the selected paths.... hm
        Contract.Requires(selector != null);

        return Implementation();
        IEnumerable<T> Implementation()
        {
            yield return item;
            foreach (var result in selector(item).TransitiveSelect(selector))
            {
                yield return result;
            }
        }
    }
    /// <summary> Transitively selects items by a specified function in a depth-first manner, but only dives into a branch if a predicate is matched. </summary>
    /// <param name="item"> The item to yield and whose elements to select transitively. </param>
    /// <param name="selector"> The function brining about the transitive relation. </param>
    /// <param name="predicate"> The function determining whether the elements of the specified items should be fetched. </param>
    public static IEnumerable<T> ConditionallyTransitiveSelect<T>(this T item, Func<T, IEnumerable<T>> selector, Func<T, bool> predicate)
    {
        yield return item;
        if (predicate(item))
        {
            foreach (var result in selector(item).SelectMany(i => i.ConditionallyTransitiveSelect(selector, predicate)))
                yield return result;
        }
    }

    /// <summary> Selects all elements in the specified sequence, and those selected by a specified function, transitively, and in a depth-first manner. </summary>
    /// <param name="sequence"> The sequence whose elements to select transitively. </param>
    /// <param name="selector"> The function brining about the transitive relation. </param>
    public static IEnumerable<T> TransitiveSelect<T>(this IEnumerable<T> sequence, Func<T, IEnumerable<T>> selector)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(selector != null);

        return Implementation();
        IEnumerable<T> Implementation()
        {
            foreach (T element in sequence)
            {
                yield return element;
                foreach (T nestedElement in TransitiveSelect(selector(element), selector))
                {
                    yield return nestedElement;
                }
            }
        }
    }
    /// <summary> Returns all indices of the elements in the specified sequence that match the specified predicate. </summary>
    [DebuggerHidden]
    public static SortedEnumerable<int> IndicesOf<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(predicate != null);

        return new SortedEnumerable<int>(sequence.indicesOf(predicate));
    }
    /// <summary> Helper method of IndicesOf. </summary>
    private static IEnumerable<int> indicesOf<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
    {
        int i = 0;
        foreach (T element in sequence)
        {
            if (predicate(element))
                yield return i;
            i++;
        }
    }

    /// <summary> Gets the indices of the specified items in the specified sequence. </summary>
    public static IEnumerable<int> IndicesOf<T>(this IEnumerable<T> sequence, IEnumerable<T> items, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(items != null);

        equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        return items.Select(item => sequence.IndexOf(item, equalityComparer));
    }


    /// <summary> Selects many items from each element in the specified sequence, and merges them into an overall sorted enumerable. </summary>
    /// <param name="sequence"> The sequence of the elements to select items from. Cannot be empty. </param>
    /// <param name="selector"> The function selecting many items per sequence element. </param>
    public static SortedEnumerable<TResult> SelectManySorted<T, TResult>(this IEnumerable<T> sequence, Func<T, ISortedEnumerable<TResult>> selector)
    {
        Contract.Requires(sequence != null);
        Contract.Requires(selector != null);

        return sequence.Select(selector).ConcatSorted();
    }

    /// <summary> Merges all elements in the specified sequences into an overall sorted enumerable. </summary>
    /// <param name="sequences"> The sequences to merge. Must contain at least one non-empty sequence, and all comparers must be equal. </param>
    public static SortedEnumerable<T> ConcatSorted<T>(this IEnumerable<ISortedEnumerable<T>> sequences)
    {
        Contract.Requires(sequences != null);
        EnsureSingleEnumerationDEBUG(ref sequences);
        Contract.LazilyAssertMinimumCount(ref sequences, 1);
        Contract.Requires(sequences.Select(sortedSequence => sortedSequence.Comparer).AreEqual());

        var comparer = ElementAt(ref sequences, 0).Comparer;

        return new SortedEnumerable<T>(sequences.concatSorted(comparer), comparer);
    }
    private static IEnumerable<T> concatSorted<T>(this IEnumerable<ISortedEnumerable<T>> sequences, Func<T, T, int> comparer)
    {
        using (var enumerators = new DisposablesList<IEnumerator<T>>(sequences.Select(sortedSequence => sortedSequence.GetEnumerator())))
        {
            Action<int> moveNext = i =>
            {
                if (!enumerators[i].MoveNext())
                {
                    enumerators.DisposeAndRemoveAt(i);
                }
            };

            Func<int> findNext = () =>
            {
                Contract.Assert(enumerators.Count >= 1);
                int indexOfMinimum = 0;
                for (int i = 1; i < enumerators.Count; i++)
                {
                    if (comparer(enumerators[indexOfMinimum].Current, enumerators[i].Current) > 0)
                    {
                        indexOfMinimum = i;
                    }
                }
                return indexOfMinimum;
            };



            for (int i = enumerators.Count - 1; i >= 0; i--)
            {
                moveNext(i);
            }

            while (enumerators.Count != 0)
            {
                int indexOfNext = findNext();
                yield return enumerators[indexOfNext].Current;
                moveNext(indexOfNext);
            }
        }
    }
    /// <summary> Returns the element at a specified index in a sequence, and substitutes the sequence such that the original sequence is guaranteed to be only enumerated once. </summary>
    /// <param name="source"> An <see cref="IEnumerable{T}"/> to return an element from. </param>
    /// <param name="index"> The index of the element to retrieve. </param>
    public static T ElementAt<T>(ref IEnumerable<T> source, int index)
    {
        Contract.Requires(source != null);
        Contract.Requires(0 <= index);

        source = source.ToLazyList();

        Contract.Requires<ArgumentOutOfRangeException>(((LazyList<T>)source).CountsAtLeast(index + 1));

        return ((LazyList<T>)source)[index];
    }
    /// <summary>
    /// Facilitates sorting a list by the specified comparer.
    /// </summary>
    public static void Sort<T>(this List<T> list, Func<T, T, int> comparer)
    {
        Contract.Requires(list != null);
        Contract.Requires(comparer != null);

        list.Sort(Comparer<T>.Create((Comparison<T>)((x, y) => comparer(x, y))));
    }
    /// <summary>
    /// Sorts both arrays in place, first on the <paramref name="sortKeys"/>, then on the <paramref name="secondarySortKeys"/>.
    /// </summary>
    public static void Sort<T, U>(T[] sortKeys, U[] secondarySortKeys, IComparer<T>? keyComparer = null, IComparer<U>? secondaryKeyComparer = null)
    {
        Contract.Requires(sortKeys is not null);
        Contract.Requires(secondarySortKeys is not null);
        Contract.Requires(sortKeys.Length == secondarySortKeys.Length);

        keyComparer ??= Comparer<T>.Default;
        secondaryKeyComparer ??= Comparer<U>.Default;
        var comparer = InterfaceWraps.ToComparer<(T, U)>((a, b) =>
        {
            int keyComparison = keyComparer.Compare(a.Item1, b.Item1);
            if (keyComparison != 0)
                return keyComparison;
            return secondaryKeyComparer.Compare(a.Item2, b.Item2);
        });

        var intermediate = sortKeys.Zip(secondarySortKeys).ToArray(sortKeys.Length);
        Array.Sort(intermediate, comparer);

        for (int i = 0; i < intermediate.Length; i++)
        {
            sortKeys[i] = intermediate[i].First;
            secondarySortKeys[i] = intermediate[i].Second;
        }
    }

    /// <summary>
    /// Sorts all 3 arrays in place, first on the <paramref name="sortKeys"/>, then on the <paramref name="secondarySortKeys"/>.
    /// </summary>
    public static void Sort<TKey, UKey, T>(this T[] items, TKey[] sortKeys, UKey[] secondarySortKeys, IComparer<TKey>? keyComparer = null, IComparer<UKey>? secondaryKeyComparer = null)
    {
        Contract.Requires(sortKeys is not null);
        Contract.Requires(secondarySortKeys is not null);
        Contract.Requires(items is not null);
        Contract.Requires(sortKeys.Length == secondarySortKeys.Length);
        Contract.Requires(sortKeys.Length == items.Length);

        keyComparer ??= Comparer<TKey>.Default;
        secondaryKeyComparer ??= Comparer<UKey>.Default;
        var comparer = InterfaceWraps.ToComparer<(TKey, UKey, T)>((a, b) =>
        {
            int keyComparison = keyComparer.Compare(a.Item1, b.Item1);
            if (keyComparison != 0)
                return keyComparison;
            return secondaryKeyComparer.Compare(a.Item2, b.Item2);
        });

        var intermediate = sortKeys.Zip(secondarySortKeys, items).ToArray(sortKeys.Length);
        Array.Sort(intermediate, comparer);

        for (int i = 0; i < intermediate.Length; i++)
        {
            sortKeys[i] = intermediate[i].First;
            secondarySortKeys[i] = intermediate[i].Second;
            items[i] = intermediate[i].Third;
        }
    }

    [DebuggerHidden]
    public static T Average<T>(this IEnumerable<T> source) where T : struct, INumber<T>
    {
        return Average<T, T, T>(source);
    }
    /// <summary>
    /// Copied from System.Linq.dll
    /// </summary>
    [DebuggerHidden]
    public static TResult Average<TSource, TAccumulator, TResult>(this IEnumerable<TSource> source)
        where TSource : struct, INumber<TSource>
        where TAccumulator : struct, INumber<TAccumulator>
        where TResult : struct, INumber<TResult>
    {

        using (IEnumerator<TSource> e = source.GetEnumerator())
        {
            if (!e.MoveNext())
            {
                throw new ArgumentException($"{nameof(source)} is empty");
            }

            TAccumulator sum = TAccumulator.CreateChecked(e.Current);
            long count = 1;
            while (e.MoveNext())
            {
                checked { sum += TAccumulator.CreateChecked(e.Current); }
                count++;
            }

            return TResult.CreateChecked(sum) / TResult.CreateChecked(count);
        }
    }
    /// <summary>
    /// Gets the standard deviation of the specified numbers.
    /// </summary>
    /// <param name="average">Provide the average as performance optimization.</param>
    public static T StandardDeviation<T>(this IEnumerable<T> numbers, T? average = null) where T : struct, INumber<T>
    {
        T μ = average ?? numbers.Average();

        int count = 0;
        T sum = T.Zero;
        foreach (var number in numbers)
        {
            sum += (number - μ) * (number - μ);
            count++;
        }
        if (count == 0)
            return T.Zero;

        var result = Math.Sqrt(Convert.ToDouble(T.CreateChecked(sum) / T.CreateChecked(count)));
        return T.CreateChecked(result);
    }
    /// <summary>
    /// Gets the standard deviation of the specified numbers.
    /// </summary>
    /// <param name="average">Provide the average as performance optimization.</param>
    public static T StandardDeviation<T>(this IEnumerable<T> numbers, out T average) where T : struct, INumber<T>
    {
        average = numbers.Average();
        return StandardDeviation(numbers, average);
    }
    /// <summary>
    /// Shuffles the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list, Random? random = null)
    {
        Contract.Requires(list != null);

        random ??= new Random(Random.Shared.Next());

        for (int n = list.Count - 1; n > 1; n--)
        {
            int k = random.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }
    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, long})"/>
    public static ulong Sum<T>(this IEnumerable<T> source, Func<T, ulong> selector)
    {
        Contract.Requires(source != null);
        Contract.Requires(selector != null);

        ulong sum = 0;
        foreach (T t in source)
        {
            ulong projected = selector(t);
            sum = checked(sum + projected);
        }
        return sum;
    }

    /// <summary>
    /// Gets an IEnumerable that throws on enumeration.
    /// </summary>
    public static IEnumerable<T> Throw<T>()
    {
        throw new UnreachableException();
#pragma warning disable CS0162 // Unreachable code detected
        yield return default; // has effect
#pragma warning restore CS0162 // Unreachable code detected
    }
    /// <summary>
    /// Gets the sequence together with the number of successive equal elements.
    /// </summary>
    public static IEnumerable<(T Element, int SuccessiveCount)> WithSuccessiveCount<T>(this IEnumerable<T> sequence, IEqualityComparer<T>? equalityComparer = null)
    {
        Contract.Requires(sequence is not null);

        equalityComparer ??= EqualityComparer<T>.Default;

        int successiveCount = 0;
        T previousElement = default!;
        bool first = true;
        foreach (T element in sequence)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (equalityComparer.Equals(previousElement, element))
                {
                    successiveCount++;
                }
                else
                {
                    successiveCount = 0;
                }
            }
            previousElement = element;
            yield return (element, successiveCount);
        }
    }
    [DebuggerHidden]
    public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> sequence, Func<TResult> selector)
    {
        return sequence.Select([DebuggerHidden] (_) => selector());
    }
    private static readonly System.Reflection.FieldInfo ReadOnlyCollection_GetUnderlyingListFieldInfo = typeof(ReadOnlyCollection<>).GetField("list", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    private static IList<T> GetUnderlyingList<T>(this IReadOnlyCollection<T> collection)
    {
        return (IList<T>)ReadOnlyCollection_GetUnderlyingListFieldInfo.GetValue(collection)!;
    }
    private static readonly System.Reflection.FieldInfo List_GetUnderlyingArrayFieldInfo = typeof(List<>).GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    private static readonly System.Reflection.FieldInfo List_GetUnderlyingSizeFieldInfo = typeof(List<>).GetField("_size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    private static T[] GetUnderlyingList<T>(this List<T> list)
    {
        return (T[])List_GetUnderlyingArrayFieldInfo.GetValue(list)!;
    }
    private static int GetUnderlyingListSize<T>(this List<T> list)
    {
        return (int)List_GetUnderlyingSizeFieldInfo.GetValue(list)!;
    }

    /// <summary>
    /// Slices without creating a copy of the data. Mutation of <paramref name="collection"/> afterwards is unsafe!
    /// </summary>
    public static IReadOnlyList<T> Slice<T>(this IReadOnlyCollection<T> collection, int start, int length)
    {
        return collection switch
        {
            null => throw new ArgumentNullException(nameof(collection)),
            ArraySegment<T> segment => segment.Slice(start, length),
            T[] array => new ArraySegment<T>(array, start, length),
            ReadOnlyCollection<T> readonlyCollection when readonlyCollection.GetUnderlyingList() is IReadOnlyCollection<T> underlyingCollection => underlyingCollection.Slice(start, length),
            List<T> list => new ArraySegment<T>(list.GetUnderlyingList(), 0, list.GetUnderlyingListSize()),
            _ => throw new NotImplementedException(collection.GetType().ToString()),
        };
    }
    /// <summary>
    /// Slices without creating a copy of the data. Mutation of <paramref name="collection"/> afterwards is unsafe!
    /// </summary>
    public static IReadOnlyList<T> Slice<T>(this IReadOnlyCollection<T> collection, Range range)
    {
        var (start, length) = range.GetOffsetAndLength(collection.Count);
        return collection.Slice(start, length);
    }
    /// <summary>
    /// Does a select on the flattened tuple elements.
    /// </summary>
    public static IEnumerable<TResult> Select<TKeyItem1, TKeyItem2, TValue, TResult>(this IEnumerable<((TKeyItem1, TKeyItem2), TValue)> sequence, Func<TKeyItem1, TKeyItem2, TValue, TResult> selector)
    {
        return sequence.Select([DebuggerHidden] (_) => selector(_.Item1.Item1, _.Item1.Item2, _.Item2));
    }
    /// <summary>
    /// Counts the occurrences of elements in the sequence.
    /// </summary>
    public static Dictionary<T, int> ToCountDictionary<T>(this IEnumerable<T> sequence, IEqualityComparer<T>? equalityComparer = null) where T : notnull
    {
        var result = new Dictionary<T, int>(equalityComparer);
        foreach (var item in sequence)
        {
            result.SetOrUpdate(item, 1, static count => count + 1);
        }
        return result;
    }
    /// <summary>
    /// Filters out all `null`s from the specified sequence.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence)
    {
        return sequence.Where(x => x != null)!;
    }
}
