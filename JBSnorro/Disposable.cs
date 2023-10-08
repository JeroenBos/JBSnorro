using JBSnorro.Diagnostics;

namespace JBSnorro;

/// <summary>
/// Executes an action whenever this class is disposed of.
/// </summary>
public class Disposable : IDisposable
{
    public static Disposable Empty { get; } = new Disposable(() => { });
    private readonly Action dispose;
    public Disposable(Action dispose)
    {
        this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
    }
    public void Dispose()
    {
        dispose();
    }
    /// <summary>
    /// Creates a disposable action, by providing both the action and its disposal function.
    public static Disposable Create(Action action, Action dispose)
    {
        action();
        return new Disposable(dispose);
    }
    /// <param name="getEnumerable">Yields exactly once</param>
    public static Disposable Create(Func<System.Collections.IEnumerable> getEnumerable)
    {
        System.Collections.IEnumerator? enumerator = null;
        return Create(_action, _dispose);
        void _action()
        {
            Contract.Requires(enumerator is null);
            enumerator = getEnumerable().GetEnumerator();
            var hasValue = enumerator.MoveNext();
            Contract.Requires(hasValue);
        }
        void _dispose()
        {
            Contract.Requires(enumerator is not null);
            var hasValue = enumerator.MoveNext();
            Contract.Requires(!hasValue);
        }
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
    public AsyncDisposable(Func<ValueTask> dispose) : this(() => dispose().AsTask())
    {
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
        return new AsyncDisposable(async Task () =>
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
        return new AsyncDisposable(async Task () =>
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
    public AsyncDisposable(T value, Func<ValueTask> dispose) : base(dispose)
    {
        this.Value = value;
    }
    public override AsyncDisposable<T> With(Func<Task> anotherDisposalTask)
    {
        return new AsyncDisposable<T>(this.Value,
            async Task () =>
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
            async Task () =>
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
