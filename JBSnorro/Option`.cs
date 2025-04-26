using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JBSnorro.Diagnostics;

namespace JBSnorro;

/// <summary> 
/// This type represents an instance of the type <code>T</code> and indicates whether it has such a value or not.
/// So it is like a nullable type, but the also for reference types and it is like option in F#. 
/// </summary>
/// <typeparam name="T"> The type of the instance to represent. </typeparam>
public readonly struct Option<T> : IEquatable<Option<T>>
{
	/// <summary> Gets the option representing no instance of type <code>T</code>. </summary>
	public static readonly Option<T> None = new Option<T>();
	/// <summary> Creates the option representing the specified instance. </summary>
	/// <param name="value"> The value represented by the returned option. </param>
	public static Option<T> Some(T value)
	{
		return new Option<T>(value);
	}

	/// <summary> The backingfield of the represented instance. Is <code>default(T)</code> for None. </summary>
	private readonly T value;
	/// <summary> Gets the value represented by this instance if it has one, or throws if it doesn't. </summary>
	public T Value
	{
		[DebuggerHidden]
		get
		{
			if (!this.HasValue)
				throw new InvalidOperationException("Option has no value");
			return this.value;
		}
	}
	/// <summary> Gets whether this instance represents an instance of type <typeparamref name="T"/>. </summary>
	[MemberNotNullWhen(returnValue: true, nameof(Value))]
	public bool HasValue { get; }
	/// <summary>
	/// Gets the value of this option, of the specified default otherwise.
	/// </summary>
	/// <param name="defaultAlternative"> The default to return in case this option does not hold a value. </param>
	[return: NotNullIfNotNull(nameof(defaultAlternative))]
	public readonly T? ValueOrDefault(T? defaultAlternative = default)
	{
		if (this.HasValue)
			return this.value;
		return defaultAlternative;
	}

	/// <summary> Creates a new option representing the specified instance. </summary>
	/// <param name="value"> The value represented by this option. </param>
	public Option(T value)
		: this()
	{
		this.value = value;
		this.HasValue = true;
	}

	public static implicit operator Option<T>(T value)
	{
		return new Option<T>(value);
	}

	/// <summary> Gets whether the specified object is equal to this option or the value held by this option, if any. </summary>
	/// <param name="obj"> The object to compare for equality against. </param>
	public override readonly bool Equals(object? obj)
	{
		if (obj is Option<T> option)
		{
			return Equals(option);
		}
		if (this.HasValue)
		{
			return object.Equals(this.value, obj);
		}
		return false;
	}
	/// <summary> Gets whether the specified option equals this option, where two values are compared with the default equality comparer. </summary>
	/// <param name="other"> The option to compare for equality against. </param>
	public readonly bool Equals(Option<T> other)
	{
		return Equals(other, EqualityComparer<T>.Default);
	}

	/// <summary> Gets whether the specified option equals this option, where two values are compared with a specified equality comparer. </summary>
	/// <param name="other"> The option to compare for equality against. </param>
	/// <param name="equalityComparer"> The equality comparer to use for comparing for equality. </param>
	public readonly bool Equals(Option<T> other, IEqualityComparer<T> equalityComparer)
	{
		Contract.Requires(equalityComparer != null);

		if (!this.HasValue)
			return !other.HasValue;
		return other.HasValue && equalityComparer.Equals(this.value, other.value);
	}

	public override string? ToString()
	{
		if (this.HasValue)
			return this.Value.ToString();
		return "None";
	}
	public override int GetHashCode()
	{
		if (this.HasValue)
		{
			return this.Value.GetHashCode();
		}
		else
		{
			return 0;
		}
	}
	public static bool operator ==(Option<T> left, Option<T> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Option<T> left, Option<T> right)
	{
		return !(left == right);
	}
}
public static class Option
{
	/// <summary>
	/// Allows omitting the type parameter to option when using <see cref="Option{T}.Some(T)"/>
	/// </summary>
	public static Option<T> Some<T>(T t)
	{
		return Option<T>.Some(t);
	}
}
