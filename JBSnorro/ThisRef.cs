using JBSnorro.Diagnostics;
using System;

namespace JBSnorro
{
	/// <summary>
	/// This type is meant to be used as constructor parameter type to optionally indicate that the constructor should treat that argument as though 'this' was specified.
	/// </summary>
	public ref struct ThisRef<T> where T : class
	{
		private readonly T value;
		public bool HasValue { get; }
		public T GetValue(object @this)
		{
			Contract.Requires(@this != null);
			Contract.Requires(this.HasValue || @this is T, "The specified 'this' reference was not of sufficiently descended type");

			if (this.HasValue)
				return this.value;
			else if (@this is T result)
				return result;
			else
				throw new DefaultSwitchCaseUnreachableException();
		}

		public static ThisRef<T> @This => default;

		public ThisRef(T value)
		{
			this.value = value;
			this.HasValue = true;
		}

		public static implicit operator ThisRef<T>(T value)
		{
			return new ThisRef<T>(value);
		}
	}
}
