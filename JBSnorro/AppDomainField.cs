using JBSnorro.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary>
	/// Represents a value that may differ between app domains. So like <see cref="ThreadStaticAttribute"/>, but then for app domains.
	/// </summary>
	public class AppDomainField<T>
	{
		// Option<T>.None means the value has been discarded, but was once present
		private readonly ConcurrentDictionary<AppDomain, Option<T>> values = new ConcurrentDictionary<AppDomain, Option<T>>();
		private readonly Func<AppDomain, T> getValue;
		private readonly bool DiscardValueOnAssemblyAdded;

		public T Value
		{
			get
			{
				var currentDomain = AppDomain.CurrentDomain;
				if (values.TryGetValue(currentDomain, out Option<T> result))
				{
					if (result.HasValue)
					{
						return result.Value;
					}
				}
				else if (this.DiscardValueOnAssemblyAdded)
				{
					currentDomain.AssemblyLoad += (sender, e) => DiscardValue(currentDomain);
				}

				var value = getValue(currentDomain);
				values[currentDomain] = value;
				return value;
			}
		}

		public AppDomainField(Func<AppDomain, T> getValue, bool discardValueOnAssemblyAdded)
		{
			Contract.Requires(getValue != null);

			this.getValue = getValue;
			this.DiscardValueOnAssemblyAdded = discardValueOnAssemblyAdded;
		}

		public void DiscardValue(AppDomain appDomain = null)
		{
			appDomain = appDomain ?? AppDomain.CurrentDomain;

			this.values[appDomain] = Option<T>.None;
		}
	}
}
