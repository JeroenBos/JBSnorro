using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Represents a value, or if unavailable, a factory to create said value. Think of this type as <code>System.Lazy&lt;T&gt;</code> without caching. </summary>
	public struct ValueOrFactory<T, TFactory>
	{
		private readonly T value;
		private readonly TFactory factory;
		/// <summary> Gets the value if it has one; throws otherwise. </summary>
		public T Value => value;
		/// <summary> Gets the factory if it has no value (and thus must have a factory); throws otherwise. </summary>
		public TFactory Factory => factory;
		/// <summary> Gets whether this object has a value; otherwise it has a factory. </summary>
		public bool HasValue => Factory == null;

		/// <summary> Creates a value or factory representing the specified value. </summary>
		public ValueOrFactory(T value)
		{
			this.value = value;
			this.factory = default(TFactory);
		}
		/// <summary> Creates a value or factory representing the specified factory. </summary>
		public ValueOrFactory(TFactory factory)
		{
			Contract.Requires(factory != null);

			this.value = default(T);
			this.factory = factory;
		}
		public static implicit operator ValueOrFactory<T, TFactory>(T value)
		{
			return new ValueOrFactory<T, TFactory>(value);
		}
		public static implicit operator ValueOrFactory<T, TFactory>(TFactory factory)
		{
			return new ValueOrFactory<T, TFactory>(factory);
		}
	}
}
