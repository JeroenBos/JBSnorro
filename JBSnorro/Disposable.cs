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
			this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
		}
		public void Dispose()
		{
			dispose();
		}
	}

	public class Disposable<T> : Disposable
	{
		public T Value { get; }
		public Disposable(T value, Action dispose) : base(dispose)
		{
			this.Value = value;
		}
		public static implicit operator T(Disposable<T> @this)
		{
			return @this.Value;
		}
	}
}
