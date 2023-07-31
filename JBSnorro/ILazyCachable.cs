using System.Diagnostics.CodeAnalysis;

namespace JBSnorro;

public interface ILazilyCachable<T>
{
	/// <summary> Gets the elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
	IEnumerable<T> Query { get; }
	/// <summary> Gets all cached elements. </summary>
	IEnumerable<T> Cache { get; }
	/// <summary> Gets all elements of the original enumerable. First yields the cached elements 
	/// and afterwards elements that haven't been yielded by the original enumerable yet and caches them. An element is cached before it is yielded. </summary>
	IEnumerable<T> Original { get; }

	/// <summary> Gets whether the end of the original sequence is reached. Beware of a subtlety: if the end has not been reached, that does not mean all elements haven't been cached. 
	/// After the last element is cached without checking if there is another element, OriginalExhausted is false while CachedCount == Original.Count(). </summary>
	bool FullyCached { get; }
	/// <summary> Gets the number of cached elements. </summary>
	int CachedCount { get; }
	/// <summary> Gets the element at the specific index, quering for more elements from the original enumerable if necessary. </summary>
	/// <param name="index"> The index of the element in the original enumerable to fetch. </param>
	T this[int index] { get; }
	/// <summary> Gets the enumerator that iterates over all cached and uncached elements. An uncached element is cached before it is yielded. </summary>
	IEnumerator<T> GetEnumerator();
	/// <summary> Tries to get the element at the specified index, returning whether it succeeded. </summary>
	/// <param name="index"> The index of the element to get. </param>
	/// <param name="value"> The element, if found. </param>
	bool TryGetAt(int index, [NotNullWhen(true)] out T? value);
}
