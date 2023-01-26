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


public class AsyncDisposable<T> : IAsyncDisposable
{
	public T Value { get; }
	private readonly Func<Task> dispose;
    public AsyncDisposable(T value, Func<Task> dispose)
	{
		this.Value = value;
		this.dispose = dispose;

	}
    public async ValueTask DisposeAsync()
    {
		await dispose();
    }
}