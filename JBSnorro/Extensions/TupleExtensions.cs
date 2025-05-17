using JBSnorro.Diagnostics;

namespace JBSnorro.Extensions;

public static class TupleExtensions
{
	/// <summary> Adds a tuple containing the two specified items to the specified list. </summary>
	public static void Add<T, U>(this IList<Tuple<T, U>> list, T item1, U item2)
	{
		Contract.Requires(list != null);

		list.Add(new Tuple<T, U>(item1, item2));
	}
	public static IEnumerable<T> ToEnumerable<T>(ValueTuple<T> tuple)
	{
		yield return tuple.Item1;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
		yield return tuple.Item4;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
		yield return tuple.Item4;
		yield return tuple.Item5;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T, T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
		yield return tuple.Item4;
		yield return tuple.Item5;
		yield return tuple.Item6;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T, T, T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
		yield return tuple.Item4;
		yield return tuple.Item5;
		yield return tuple.Item6;
		yield return tuple.Item7;
	}
	public static IEnumerable<T> ToEnumerable<T>((T, T, T, T, T, T, T, T) tuple)
	{
		yield return tuple.Item1;
		yield return tuple.Item2;
		yield return tuple.Item3;
		yield return tuple.Item4;
		yield return tuple.Item5;
		yield return tuple.Item6;
		yield return tuple.Item7;
		yield return tuple.Item8;
	}

	public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this (TKey, TValue) tuple)
	{
		return new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);
	}
}
