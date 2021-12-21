using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	class IsEmptyEnumerable<T> : IEnumerable<T>
	{
		private readonly IEnumerable<T> source;
		private readonly IEnumerator<T> enumerator;
		private readonly bool isEmpty;

		private bool firstEnumeratorHasBeenReturned;
		public IsEmptyEnumerable(IEnumerable<T> source, out bool isEmpty)
		{
			this.source = source;
			this.enumerator = new IsEmptyEnumerator(this);
			try
			{
				this.isEmpty = isEmpty = !enumerator.MoveNext();
			}
			catch
			{
				enumerator.Dispose();
				throw;
			}
			if (isEmpty)
			{
				enumerator.Dispose();
			}
		}
		public IEnumerator<T> GetEnumerator()
		{
			if (firstEnumeratorHasBeenReturned)
			{
				firstEnumeratorHasBeenReturned = true;
				return enumerator;
			}
			else
			{
				return source.GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private struct IsEmptyEnumerator : IEnumerator<T>
		{
			private readonly IsEmptyEnumerable<T> owner;
			private bool first;

			public IsEmptyEnumerator(IsEmptyEnumerable<T> owner)
			{
				this.first = true;
				this.owner = owner;
			}

			public T Current => owner.enumerator.Current;

			object IEnumerator.Current => Current;

			public void Dispose()
			{
				owner.enumerator.Dispose();
			}

			public bool MoveNext()
			{
				if (first)
				{
					first = false;
					return owner.isEmpty;
				}
				else
				{
					return owner.enumerator.MoveNext();
				}
			}

			public void Reset()
			{
				owner.enumerator.Reset();
			}
		}
	}
}
