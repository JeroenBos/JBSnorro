using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> Represents a collection of disposable elements, each of which is disposed upon disposal of this collection. </summary>
	public sealed class DisposablesList<T> : List<T>, IDisposable where T : IDisposable
	{
		public DisposablesList() { }
		public DisposablesList(int initialCapacity) : base(initialCapacity) { }
		public DisposablesList(IEnumerable<T> initialElements) : base(initialElements) { }

		public void Dispose()
		{
			foreach (T element in this)
				if (element != null)
					element.Dispose();
		}

		public void DisposeAndRemoveAt(int index)
		{
			this[index].Dispose();
			this.RemoveAt(index);
		}
		public void DisposeAndRemoveLast()
		{
			this.Last().Dispose();
			this.RemoveLast();
		}
	}
}
