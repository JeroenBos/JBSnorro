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
		public static IDisposable Create(Action disposeAction) => new Disposable(disposeAction);
		public static IAsyncDisposable Create(Func<Task> disposeAction) => new AsyncDisposable(disposeAction);



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

	/// <summary>
	/// Executes an async action whenever this class is disposed of.
	/// </summary>
	public class AsyncDisposable : IAsyncDisposable
	{
		private readonly Func<Task> dispose;
		public AsyncDisposable(Func<Task> dispose)
		{
			this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
		}

        public async ValueTask DisposeAsync()
        {
			await dispose();
        }
    }

	public class AsyncDisposable<T> : AsyncDisposable
	{
		public T Value { get; }
		public AsyncDisposable(T value, Func<Task> dispose) : base(dispose)
		{
			this.Value = value;
		}
        public static implicit operator T(AsyncDisposable<T> @this)
        {
            return @this.Value;
        }
    }
}
