using System;
using System.Collections.Generic;

namespace JBSnorro.Collections.Sorted
{
	/// <summary> Gets an enumerable ordered against the specified comparer. </summary>
	public interface ISortedEnumerable<T> : IEnumerable<T>
	{
		/// <summary> The comparer this enumerable is ordered against. </summary>
		Func<T, T, int> Comparer { get; }
	}
}
