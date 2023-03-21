using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Extensions
{
    /// <summary> Contains extension methods for <see cref="Dictionary{TKey, TValue}"/>. </summary>
    public static class DictionaryExtensions
    {

        /// <summary> Gets the value in the dictionary associated with the specified key and adds (and returns) the specified value if it isn't present yet. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueToAdd"> The value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static object GetOrAdd(this IDictionary dictionary, object key, object valueToAdd)
        {
            Contract.Requires(dictionary != null);

            object result;
            ContainsOrAdd(dictionary, key, _ => valueToAdd, out result);
            return result;
        }
        /// <summary> Gets the value in the dictionary associated with the specified key and adds (and returns) the specified value if it isn't present yet. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueToAdd"> The value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue valueToAdd)
        {
            Contract.Requires(dictionary != null);

            TValue result;
            ContainsOrAdd(dictionary, key, _ => valueToAdd, out result);
            return result;
        }

        /// <summary> Gets the value in the dictionary associated with the specified key and adds (and returns) a value based on the key if it isn't present yet. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueSelector"> selects to value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static object GetOrAdd(this IDictionary dictionary, object key, Func<object, object> valueSelector)
        {
            Contract.Requires(dictionary != null);
            Contract.Requires(valueSelector != null);

            object result;
            ContainsOrAdd(dictionary, key, valueSelector, out result);
            return result;

        }
        /// <summary> Gets the value in the dictionary associated with the specified key and adds (and returns) a value based on the key if it isn't present yet. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueSelector"> selects to value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueSelector)
        {
            Contract.Requires(dictionary != null);
            Contract.Requires(valueSelector != null);

            TValue result;
            ContainsOrAdd(dictionary, key, valueSelector, out result);
            return result;

        }
        /// <summary> Gets the value that is stored in the dictionary associated with the specified key, or gets the default value otherwise. </summary>
        /// <typeparam name="TKey"> The key type of the dictionary. </typeparam>
        /// <typeparam name="TValue"> The value type of the dictionary. </typeparam>
        /// <param name="dictionary"> The dictionary to search in/ </param>
        /// <param name="key"> The key of the value to get. </param>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.GetValueOrDefault(key, default(TValue));
        }
        /// <summary> Gets the value that is stored in the dictionary associated with the specified key, or gets the default value otherwise. </summary>
        /// <typeparam name="TKey"> The key type of the dictionary. </typeparam>
        /// <typeparam name="TValue"> The value type of the dictionary. </typeparam>
        /// <param name="dictionary"> The dictionary to search in/ </param>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="defaultValue"> The default value to be returned in case the dictionary doesn't contain the specified key. </param>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            Contract.Requires(dictionary != null);

            TValue result;
            if (dictionary.TryGetValue(key, out result))
                return result;
            return defaultValue;
        }
        /// <summary> Gets whether the specified key in the dictionary exists and adds the specified value if it isn't.
        /// Also yields the associated value via an out parameter. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueToAdd"> The value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static bool ContainsOrAdd(this IDictionary dictionary, object key, object valueToAdd, out object result)
        {
            Contract.Requires(dictionary != null);

            return dictionary.ContainsOrAdd(key, _ => valueToAdd, out result);
        }
        /// <summary> Gets whether the specified key in the dictionary exists and adds the specified value if it isn't.
        /// Also yields the associated value via an out parameter. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueToAdd"> The value that is added when the specified key is not present. </param>
        [DebuggerHidden]
        public static bool ContainsOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue valueToAdd, out TValue result)
        {
            Contract.Requires(dictionary != null);

            return dictionary.ContainsOrAdd(key, _ => valueToAdd, out result);
        }
        /// <summary> Gets whether the specified key in the dictionary exists and adds a value based on the key if it isn't.
        /// Also yields the associated value via an out parameter. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueSelector"> selects to value that is added when the specified key is not present. </param>
        public static bool ContainsOrAdd(this IDictionary dictionary, object key, Func<object, object> valueSelector, out object result)
        {
            Contract.Requires(dictionary != null);
            Contract.Requires(valueSelector != null);

            if (!dictionary.Contains(key))
            {
                result = valueSelector(key);
                dictionary.Add(key, result);
                return false;
            }
            result = dictionary[key];
            return true;
        }
        /// <summary> Gets whether the specified key in the dictionary exists and adds a value based on the key if it isn't.
        /// Also yields the associated value via an out parameter. </summary>
        /// <param name="dictionary"> The dictionary in which to get the value. </param>
        /// <param name="key"> The key to use for lookup. </param>
        /// <param name="valueSelector"> selects to value that is added when the specified key is not present. </param>
        public static bool ContainsOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueSelector, out TValue result)
        {
            Contract.Requires(dictionary != null);
            Contract.Requires(valueSelector != null);

            if (!dictionary.TryGetValue(key, out result))
            {
                result = valueSelector(key);
                dictionary.Add(key, result);
                return false;
            }
            return true;
        }

        /// <summary> Maps the list to a dictionary (given a key selector and value selector) and maintains the mapping when the list changes. </summary>
        public static IDictionary<TKey, TValue> ToLiveDictionary<T, TKey, TValue>(this ObservableCollection<T> list,
                                                                                       Func<T, TKey> keySelector,
                                                                                       Func<T, TValue> valueSelector,
                                                                                       IEqualityComparer<TKey> equalityComparer = null)
        {
            Contract.Requires(list != null);
            Contract.Requires(keySelector != null);
            Contract.Requires(valueSelector != null);

            Dictionary<TKey, TValue> result = equalityComparer == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(equalityComparer);

            NotifyCollectionChangedEventHandler changedHandler = (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var newItem in e.NewItems.Cast<T>())
                        {
                            var key = keySelector(newItem);
                            var value = valueSelector(newItem);

                            result.Add(key, value);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        throw new NotImplementedException();
                    case NotifyCollectionChangedAction.Reset:
                        result.Clear();
                        break;
                    default:
                        throw new DefaultSwitchCaseUnreachableException();
                }
            };

            list.CollectionChanged += changedHandler;

            if (list.Count != 0)
            {
                changedHandler(list, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)list));
            }

            return result;
        }

        /// <summary>
        /// Creates a readonly dictionary by mapping the specified sequence to keys and values.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> sequence, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            return sequence.ToReadOnlyDictionary(keySelector, (source, key) => valueSelector(source), equalityComparer);
        }
        /// <summary>
        /// Creates a readonly dictionary by mapping the specified sequence to keys and values.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> sequence, Func<TSource, TKey> keySelector, Func<TSource, TKey, TValue> valueSelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(keySelector != null);
            Contract.Requires(valueSelector != null);

            equalityComparer = equalityComparer ?? EqualityComparer<TKey>.Default;

            return implementation();
            IReadOnlyDictionary<TKey, TValue> implementation()
            {
                int capacity = (sequence as ICollection)?.Count ?? 4;
                var result = new Dictionary<TKey, TValue>(capacity, equalityComparer);

                foreach (var source in sequence)
                {
                    TKey key = keySelector(source);
                    TValue value = valueSelector(source, key);

                    result[key] = value;
                }

                return result;
            }
        }
#nullable enable
        /// <summary> Compares the specified dictionaries for equality.</summary>
        public static bool ContentEquals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary,
                                                       [NotNullWhen(true)] IReadOnlyDictionary<TKey, TValue>? other)
        {
            Contract.Requires(dictionary != null);

            if (ReferenceEquals(dictionary, other))
                return true;
            if (dictionary.Count != other?.Count)
                return false;

            return other.OrderBy(kvp => kvp.Key)
                        .SequenceEqual(dictionary.OrderBy(kvp => kvp.Key));
        }
        /// <summary> Creates a new dictionary by mapping all values onto new values. </summary>
        public static Dictionary<TKey, TResultValue> Map<TKey, TValue, TResultValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            Func<TKey, TValue, TResultValue> map) where TKey : notnull
        {
            Contract.Requires(dictionary != null);
            Contract.Requires(map != null);

            return dictionary.Select(kvp => KeyValuePair.Create(kvp.Key, map(kvp.Key, kvp.Value)))
                              .ToDictionary();
        }

        /// <summary>
        /// Converts a tuple to key value pair.
        /// </summary>
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this (TKey, TValue) tuple)
        {
            return new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);
        }
        /// <summary>
        /// Converts the tuple to a key-value pair.
        /// </summary>
        public static (TKey, TValue) ToKeyValuePair<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
        {
            return (kvp.Key, kvp.Value);
        }
        /// <summary>
        /// Enumerates the dictionary <see cref="KeyValuePair{TKey, TValue}"/>s as <see cref="(TKey, TValue)"/>-tuples.
        /// </summary>
        public static IEnumerable<(TKey, TValue)> AsTuples<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull
        {
            return dict.Select(DictionaryExtensions.ToKeyValuePair<TKey, TValue>);
        }
    }
}
