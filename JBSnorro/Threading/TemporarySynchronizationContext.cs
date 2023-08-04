using JBSnorro.Diagnostics;

namespace JBSnorro.Threading;

public class TemporarySynchronizationContext : IDisposable
{
	private readonly SynchronizationContext? originalContext;
	private readonly SynchronizationContext temporaryContext;
	public TemporarySynchronizationContext(SynchronizationContext context)
	{
		Contract.Requires(context != null);

		this.originalContext = SynchronizationContext.Current;
		this.temporaryContext = context;
		SynchronizationContext.SetSynchronizationContext(context);
	}
	public void Dispose()
	{
		if (SynchronizationContext.Current != temporaryContext)
			throw new InvalidOperationException("The original synchronization context could not be reinstated");

		SynchronizationContext.SetSynchronizationContext(this.originalContext);
	}
}
public static class TemporarySynchronizationContextExtensions
{
	/// <summary> Sets the synchronization context to the specified context, and reinstates the original context upon disposal. </summary>
	public static IDisposable AsTemporarySynchronizationContext(this SynchronizationContext context) => new TemporarySynchronizationContext(context);
}
