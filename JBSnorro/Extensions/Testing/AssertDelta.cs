using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.Diagnostics
{
	public class AssertDelta : IDisposable
	{
		private readonly Func<int> getCurrentValue;
		private readonly int expectedDelta;
		private readonly int originalCount;
		public AssertDelta(int expectedDelta, Func<int> getCurrentValue)
		{
			Contract.Requires(getCurrentValue != null);

			this.getCurrentValue = getCurrentValue;
			this.expectedDelta = expectedDelta;
			this.originalCount = getCurrentValue();
		}
		public void Dispose()
		{
			int newCount = this.getCurrentValue();
			Contract.Ensures(originalCount + expectedDelta == newCount);
		}
	}
}
