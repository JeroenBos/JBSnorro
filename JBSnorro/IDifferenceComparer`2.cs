#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JBSnorro
{
	/// <summary> An equality comparer that can also list differences. </summary>
	public interface IDifferenceComparer<T, TDiff> : IEqualityComparer<T>
	{
		IEnumerable<TDiff> ComputeDifference(T x, T y);

		bool IEqualityComparer<T>.Equals([AllowNull] T x, [AllowNull] T y)
		{
			if (ReferenceEquals(x, y))
				return true;
			if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
				return false;
			return !ComputeDifference(x, y).Any();
		}

		int IEqualityComparer<T>.GetHashCode(T obj) => throw new InvalidOperationException("Not supported");
	}
}
