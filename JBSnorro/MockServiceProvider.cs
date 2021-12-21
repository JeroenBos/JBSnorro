using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro
{
	/// <summary>
	/// Creates a service provider implemented as immutable dictionary from type to implementation.
	/// </summary>
	public class MockServiceProvider : IServiceProvider
	{
		public static MockServiceProvider.Builder Create()
		{
			return new Builder();
		}

		private readonly IReadOnlyDictionary<Type, object> implementations;
		private MockServiceProvider(IReadOnlyDictionary<Type, object> implementations) => this.implementations = implementations;

		public object GetService(Type serviceType)
		{
			if (serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));

			if (!this.implementations.ContainsKey(serviceType))
				throw new InvalidOperationException("No implementation was provided for type " + serviceType.Name);

			return this.implementations[serviceType];
		}

		public class Builder
		{
			private readonly Dictionary<Type, object> implementations = new Dictionary<Type, object>();
			public Builder Add<TKey>(TKey implementation)
			{
				this.implementations.Add(typeof(TKey), implementation);
				return this;
			}
			public Builder Add(Type key, object implementation)
			{
				Contract.Assert<NotImplementedException>(implementation != null); // TODO: improve assertion below
				Contract.Assert(key.IsAssignableFrom(implementation.GetType()), "The specified implementation is not assignable to the specified type key");

				this.implementations.Add(key, implementation);
				return this;
			}

			public MockServiceProvider Build()
			{
				return new MockServiceProvider(this.implementations.ToDictionary());
			}
		}
	}

	public class DelegatingServiceProvider : IServiceProvider
	{
		private IServiceProvider serviceProvider;
		public IServiceProvider ServiceProvider
		{
			get => this.serviceProvider;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				this.serviceProvider = value;
			}
		}

		public DelegatingServiceProvider(IServiceProvider initialProvider = null)
		{
			this.serviceProvider = initialProvider;
		}

		public object GetService(Type serviceType)
		{
			if (this.serviceProvider == null)
				throw new InvalidOperationException("Cannot get services of this delegating service provider when no service provider has been set");

			return this.serviceProvider.GetService(serviceType);
		}
	}
}
