using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro
{
	/// <summary>
	/// Executes an action whenever this class is disposed of.
	/// </summary>
	public class Disposable : IDisposable
	{
		private readonly Action dispose;
		public Disposable(Action dispose)
		{
			this.dispose = dispose;
		}
		public void Dispose()
		{
			dispose();
		}
	}
}
