using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Contains conversion methods from delegates to some interfaces, effectively wrapping the delegate. </summary>
	public static class InterfaceWraps
	{
		/// <summary> Converts the specified delegate to an IComparer&lt;<typeparamref name="T"/>&gt; </summary>
		/// <typeparam name="T"> The type of the items to compare. </typeparam>
		/// <param name="comparer"> The delegate to wrap the interface around. </param>
		[DebuggerHidden]
		public static IComparer<T> ToComparer<T>(this Func<T, T, int> comparer)
		{
			Contract.Requires(comparer != null);

			return new SimpleIComparer<T>(comparer);
		}
		/// <summary> Converts the specified delegate to an IEqualityComparer&lt;<typeparamref name="T"/>&gt; </summary>
		/// <typeparam name="T"> The type of the items to compare. </typeparam>
		/// <param name="comparer"> The delegate to wrap the interface around. </param>
		[DebuggerHidden]
		public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, bool> comparer)
		{
			Contract.Requires(comparer != null);

			return new SimpleIEqualityComparer<T>(comparer);
		}
		/// <summary> Converts the specified delegate to an IEqualityComparer&lt; &gt;, and allows to provide a GetHashCode function as well. </summary>
		[DebuggerHidden]
		public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, bool> comparer, Func<T, int> getHashCode)
		{
			Contract.Requires(comparer != null);

			return new SimpleIEqualityComparer<T>(comparer, getHashCode);
		}
		/// <summary> Creates an equality comparer from the specified comparer, by having equality when the comparer returns 0, and inequality otherwise. </summary>
		public static IEqualityComparer<T> ToEqualityComparer<T>(this IComparer<T> comparer)
		{
			return new SimpleIEqualityComparer<T>((a, b) => 0 == comparer.Compare(a, b));
		}
		/// <summary> Returns the specified comparer if non-null; or otherwise a default comparer (and asserts it exists). </summary>
		[DebuggerHidden]
		public static Func<T, T, int> OrDefault<T>(this Func<T, T, int> comparer)
		{
			if (comparer != null)
			{
				return comparer;
			}

			AssertDefaultComparerExists<T>();

			return Comparer<T>.Default.Compare;
		}
		/// <summary> Returns the specified comparer if non-null; or otherwise a default comparer (and asserts it exists). </summary>
		[DebuggerHidden]
		public static IComparer<T> OrDefault<T>(this IComparer<T> comparer)
		{
			if (comparer != null)
			{
				return comparer;
			}

			AssertDefaultComparerExists<T>();

			return Comparer<T>.Default;
		}
		/// <summary> Throws if the default comparer doesn't exist. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		private static void AssertDefaultComparerExists<T>()
		{
			Contract.Assert(typeof(T).Implements(typeof(IComparable)) || typeof(T).Implements(typeof(IComparable<>)), "No comparer was specified, and no default comparer was found");
		}

		/// <summary> Wraps around a <code>Func&lt;T, T&gt;</code> to represent an IComparer&lt;T&gt;. </summary>
		/// <typeparam name="T"> The type of the elements to compare. </typeparam>
		private sealed class SimpleIComparer<T> : IComparer<T>
		{
			/// <summary> The underlying comparing delegate. </summary>
			[NotNull]
			private readonly Func<T, T, int> compare;
			/// <summary> Creates a new IComparer&lt;<typeparamref name="T"/>&gt; from the specified delegate. </summary>
			/// <param name="compare"> The function comparing two elements. </param>
			[DebuggerHidden]
			public SimpleIComparer([NotNull] Func<T, T, int> compare)
			{
				Contract.Requires(compare != null);

				this.compare = compare;
			}
			/// <summary> Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other. </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns> A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.
			/// Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>. </returns>
			[DebuggerHidden]
			public int Compare(T x, T y)
			{
				return compare(x, y);
			}
		}
#nullable enable
		/// <summary> Wraps around a <code>Func&lt;T, T&gt;</code> to represent an IComparer&lt;T&gt;. </summary>
		/// <typeparam name="T"> The type of the elements to compare. </typeparam>
		private sealed class SimpleIEqualityComparer<T> : IEqualityComparer<T>
		{
			/// <summary> The underlying comparing delegate. </summary>
			private readonly Func<T, T, bool> equalityComparer;

			private readonly Func<T, int>? getHashCode;
			/// <summary> Creates a new IEqualityComparer&lt;<typeparamref name="T"/>&gt; from the specified delegate, using the GetHashCode method implemented by <typeparamref name="T"/>. </summary>
			/// <param name="equalityComparer"> The function comparing two elements. </param>
			/// <param name="getHashCode"> A function that determines the hash code of an object of type <typeparamref name="T"/>. Specify null to use the default. </param>
			[DebuggerHidden]
			public SimpleIEqualityComparer(Func<T?, T?, bool> equalityComparer, Func<T, int>? getHashCode = null)
			{
				this.equalityComparer = equalityComparer;
				this.getHashCode = getHashCode;
			}
			/// <summary> Determines whether the specified objects are equal. </summary>
			/// <param name="x">The first object of type <typeparamref name="T"/> to compare.</param><param name="y">The second object of type <typeparamref name="T"/> to compare.</param>
			[DebuggerHidden]
			public bool Equals(T? x, T? y)
			{
				return equalityComparer(x!, y!);
			}
			/// <summary> Returns a hash code for the specified object. </summary>
			/// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
			[DebuggerHidden]
			public int GetHashCode(T obj)
			{
				if (getHashCode != null)
					return getHashCode(obj);
				if (obj == null)
					return 0;
				return obj.GetHashCode();
			}
		}
	}
}
