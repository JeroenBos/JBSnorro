namespace JBSnorro;

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


/// <summary>
/// Executes an action whenever this class is disposed of.
/// </summary>
public class AsyncDisposable : IAsyncDisposable
{
	protected readonly Func<Task> dispose;
	public AsyncDisposable(Func<Task> dispose)
	{
		this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
	}
	public virtual async ValueTask DisposeAsync()
	{
		await dispose();
	}

	public virtual AsyncDisposable With(Func<Task> anotherDisposalTask)
	{
		return new AsyncDisposable(() => Task.WhenAll(this.dispose(), anotherDisposalTask()));
	}
	public virtual AsyncDisposable WithAfter(Func<Task> anotherDisposalTask)
	{
		return new AsyncDisposable(async () =>
		{
			try
			{
				await this.dispose();
			}
			finally
			{
				await anotherDisposalTask();
			}
		});
	}
	public virtual AsyncDisposable WithBefore(Func<Task> anotherDisposalTask)
	{
		return new AsyncDisposable(async () =>
		{
			try
			{
				await anotherDisposalTask();
			}
			finally
			{
				await this.dispose();
			}
		});
	}
}


/// <summary>
/// Represents a task and a clean up method after that task has finished.
/// </summary>
public class DisposableTaskOutcome : IAsyncDisposable
{
	public Task Task { get; }
	public IAsyncDisposable Disposable { get; }

	public DisposableTaskOutcome(Task task, IAsyncDisposable disposable)
	{
		this.Task = task;
		this.Disposable = disposable;
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			await Task;
		}
		finally
		{
			await Disposable.DisposeAsync();
		}
	}
}

public class DisposableTaskOutcome<T> : DisposableTaskOutcome
{
	public new Task<T> Task => (Task<T>)base.Task;

	public DisposableTaskOutcome(Task<T> task, AsyncDisposable disposable) : base(task, disposable)
	{
	}
}


public class AsyncDisposable<T> : AsyncDisposable
{
	public T Value { get; }
	public AsyncDisposable(T value, Func<Task> dispose) : base(dispose)
	{
		this.Value = value;

	}
	public override AsyncDisposable<T> With(Func<Task> anotherDisposalTask)
	{
		 return new AsyncDisposable<T>(this.Value,
			 async () =>
			 {
				 try
				 {
					 await this.dispose();
				 }
				 finally
				 {
					 await anotherDisposalTask();
				 }
			 });
	}
	public override AsyncDisposable<T> WithAfter(Func<Task> anotherDisposalTask)
	{
		return new AsyncDisposable<T>(this.Value,
			async () =>
			{
				try
				{
					await anotherDisposalTask();
				}
				finally
				{
					await this.dispose();
				}
			});
	}
}