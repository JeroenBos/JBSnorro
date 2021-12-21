using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Encapsulates an enumerator such that it can be used as an enumerable, one that can iterated over only once. </summary>
	internal sealed class EnumeratorWrapper<T> : IEnumerable<T>
	{
		private readonly IEnumerator<T> enumerator;
		/// <summary> Indicates whether this enumerator has started enumerating its elements, in which case starting a second enumeration should fail. </summary>
		private bool enumerated;
		private readonly bool? includeCurrent;

		/// <param name="includeCurrent"> Indicates whether <code>enumerator.MoveNext()</code> yielded true and the currently pointed sequence element should be yielded. </param>
		public EnumeratorWrapper(IEnumerator<T> enumerator, bool? includeCurrent)
		{
			Contract.Requires(enumerator != null);

			this.enumerator = enumerator;
			this.includeCurrent = includeCurrent;
		}
		[DebuggerHidden]
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			if (enumerated)
				throw new InvalidOperationException("A singly enumerable cannot be enumerated multiple times");
			enumerated = true;
			return new Wrapper(this.enumerator, includeCurrent);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}
		private sealed class Wrapper : IEnumerator<T>
		{
			private readonly IEnumerator<T> enumerator;
			private bool? includeCurrent;

			public Wrapper(IEnumerator<T> enumerator, bool? includeCurrent)
			{
				this.enumerator = enumerator;
				this.includeCurrent = includeCurrent;
			}

			public T Current => enumerator.Current;
			object IEnumerator.Current => Current;
			public void Dispose()
			{
				enumerator.Dispose();
			}
			public bool MoveNext()
			{
				if (this.includeCurrent == null) // if last call to MoveNext returned false
				{
					this.includeCurrent = false;
					return false;
				}
				else if (this.includeCurrent.Value) // there is a current, and it needs to be yielded first
				{
					this.includeCurrent = false;
					return true;
				}
				else
				{
					return enumerator.MoveNext();
				}
			}
			public void Reset()
			{
				throw new InvalidOperationException();
			}
		}
	}
}
