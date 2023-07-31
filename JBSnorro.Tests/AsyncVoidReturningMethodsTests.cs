using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NitoAsyncContext = Nito.AsyncEx.AsyncContext;

namespace Tests.JBSnorro;

[TestClass]
public class AsyncVoidReturningMethodsTests
{
	[TestMethod]
	public void ATest()
	{
		bool continued = false;
		async void action()
		{
			await Task.Delay(100);
			continued = true;
		}
		Nito.AsyncEx.AsyncContext.Run(action);
		Contract.Assert(continued);
	}
	[TestMethod]
	public void I()
	{
		var originalContext = SynchronizationContext.Current;
		try
		{
			SynchronizationContext.SetSynchronizationContext(new NitoAsyncContext().SynchronizationContext);
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(originalContext);
		}
	}
}
