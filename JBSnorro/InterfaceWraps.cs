using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro;

/// <summary> Contains conversion methods from delegates to some interfaces, effectively wrapping the delegate. </summary>
public static class InterfaceWraps
{
    [DebuggerHidden]
    public static IComparer<T> GetReversedComparer<T>() where T : notnull, IComparable<T>
    {
        return ToComparer<T>([DebuggerHidden] (a, b) => b!.CompareTo(a));
    }
    /// <summary> Converts the specified delegate to an IComparer&lt;<typeparamref name="T"/>&gt; </summary>
    /// <typeparam name="T"> The type of the items to compare. </typeparam>
    /// <param name="comparer"> The delegate to wrap the interface around. </param>
    [DebuggerHidden]
    public static IComparer<T> ToComparer<T>(this Func<T?, T?, int> comparer) 
    {
        // basically the nullability of this method is not encodable in C#. I'd want the returned generic type parameter to be null only if the passed in one is null.
        // but that's also because IComparer<T> is configured to always accept nulls.
        // A workaround is to apply a ! to the argument (but only if it is of type Func<T, T, int>)
        Contract.Requires(comparer != null);

        return new SimpleIComparer<T>(comparer);
    }
    /// <summary> Converts the specified delegate to an IEqualityComparer&lt;<typeparamref name="T"/>&gt; </summary>
    /// <typeparam name="T"> The type of the items to compare. </typeparam>
    /// <param name="comparer"> The delegate to wrap the interface around. </param>
    [DebuggerHidden]
    public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T?, T?, bool> comparer)
    {
        Contract.Requires(comparer != null);

        return new SimpleIEqualityComparer<T>(comparer);
    }
    /// <summary> Converts the specified delegate to an IEqualityComparer&lt; &gt;, and allows to provide a GetHashCode function as well. </summary>
    [DebuggerHidden]
    public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T?, T?, bool> comparer, Func<T, int> getHashCode)
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
    public static Func<T, T, int> OrDefault<T>(this Func<T, T, int>? comparer)
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
    public static IComparer<T> OrDefault<T>(this IComparer<T>? comparer)
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
    public static IEqualityComparer<object?> GetDefaultEqualityComparer(this Type type)
    {
        return new MyOwnDefaultEqualityComparer { ActualType = type };
    }
    public static IEqualityComparer<T> GetIEquatableEqualityComparer<T>() where T : IEquatable<T>
    {
        return new EqualityComparerThroughIEquatable<T>();
    }
    public static IEqualityComparer<object?> GetIEquatableEqualityComparer(this Type typeImplementingIEquatable)
    {
        return EqualityComparerThroughIEquatable.Create(typeImplementingIEquatable);
    }
    /// <summary>
    /// Creates a comparer of <typeparamref name="T"/> by comparing them using comparable comparison tokens.
    /// </summary>
    [DebuggerHidden]
    public static IComparer<T> ComparerBy<T, TComparison>(Func<T, TComparison> getComparisonToken) where TComparison : IComparable<TComparison>
    {
        return new ComparerByImpl<T, TComparison>(getComparisonToken);
    }
    private sealed class ComparerByImpl<T, TComparison> : IComparer<T> where TComparison : IComparable<TComparison>
    {
        private readonly Func<T, TComparison> getComparisonToken;
        [DebuggerHidden]
        public ComparerByImpl(Func<T, TComparison> getComparisonToken)
        {
            this.getComparisonToken = getComparisonToken;
        }
        [DebuggerHidden]
        public int Compare(T? x, T? y)
        {
            return getComparisonToken(x!).CompareTo(getComparisonToken(y!));
        }
    }
    /// <summary> Wraps around a <code>Func&lt;T, T&gt;</code> to represent an IComparer&lt;T&gt;. </summary>
    /// <typeparam name="T"> The type of the elements to compare. </typeparam>
    private sealed class SimpleIComparer<T> : IComparer<T>
    {
        /// <summary> The underlying comparing delegate. </summary>
        private readonly Func<T?, T?, int> compare;
        /// <summary> Creates a new IComparer&lt;<typeparamref name="T"/>&gt; from the specified delegate. </summary>
        /// <param name="compare"> The function comparing two elements. </param>
        [DebuggerHidden]
        public SimpleIComparer(Func<T?, T?, int> compare)
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
        public int Compare(T? x, T? y)
        {
            return compare(x, y);
        }
    }
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
    private sealed class MyOwnDefaultEqualityComparer : IEqualityComparer<object?>
    {
        public required Type ActualType { get; init; }
        public new bool Equals(object? x, object? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
    private sealed class EqualityComparerThroughIEquatable<T> : IEqualityComparer<T> where T : IEquatable<T>
    {
        public bool Equals(T? x, T? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class EqualityComparerThroughIEquatable : IEqualityComparer<object?>
    {
        public static IEqualityComparer<object?> Create(Type t) => new EqualityComparerThroughIEquatable(t);
        private readonly static Cache<Type, Delegate> iequatableEquals = Cache<Type, Delegate>.CreateThreadSafe(getIEquatable_Equals);
        private static Delegate getIEquatable_Equals(Type type)
        {
            if (!type.Implements(typeof(IEquatable<>)))
                throw new ArgumentException($"{type.FullName} doesn't implement 'System.IEquatable<>'.", nameof(type));
            if (!type.Implements(typeof(IEquatable<>).MakeGenericType(type)))
                throw new ArgumentException($"{type.FullName} doesn't implement 'System.IEquatable<{type.FullName}>'.", nameof(type));

            var equalsMethod = typeof(IEquatable<>).MakeGenericType(type).GetMethod("Equals")!;

            bool Equals(object? x, object? y)
            {
                return (bool)equalsMethod.Invoke(x, new object?[] { y })!;
            }
            return Equals;
        }

        private readonly Delegate equals;
        private EqualityComparerThroughIEquatable(Type t)
        {
            this.equals = iequatableEquals[t];
            this.ActualType = t;
        }
        public Type ActualType { get; }
        public new bool Equals(object? x, object? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return (bool)this.equals.DynamicInvoke(x, y)!;
        }

        public int GetHashCode([DisallowNull] object obj)
        {
            if (obj is null)
            {
                return 0;
            }
            if (obj.GetType() != typeof(object) && !TypeExtensions.OverridesGetHashCode(obj))
            {
                throw new ArgumentException($"'{ActualType.FullName}' doesn't override 'object.GetHashCode'", "type");
            }

            return obj.GetHashCode();
        }
    }

    public static IEqualityComparer<T> Map<T, TComparable>(this IEqualityComparer<TComparable> equalityComparer, Func<T, TComparable> comparableSelector)
    {
        return new MappedEqualityComparer<TComparable, T>(equalityComparer, comparableSelector);
    }

    sealed class MappedEqualityComparer<TComparable, T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<TComparable> equalityComparer;
        private readonly Func<T, TComparable> comparableSelector;

        public MappedEqualityComparer(IEqualityComparer<TComparable> equalityComparer, Func<T, TComparable> comparableSelector)
        {
            this.equalityComparer = equalityComparer;
            this.comparableSelector = comparableSelector;
        }

        public bool Equals(T? x, T? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;
            return this.equalityComparer.Equals(comparableSelector(x), comparableSelector(y));
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return this.equalityComparer.GetHashCode(comparableSelector(obj)!);
        }
    }

}
