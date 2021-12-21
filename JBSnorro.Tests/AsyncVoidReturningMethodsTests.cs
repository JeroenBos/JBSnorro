using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NitoAsyncContext = Nito.AsyncEx.AsyncContext;

namespace JBSnorro.Tests
{
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
}
