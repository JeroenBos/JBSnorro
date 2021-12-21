using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class WeakReferenceDictionaryTests
	{
		static void waitForGC()
		{
			GC.WaitForPendingFinalizers();
			Thread.Sleep(100); // I had expected not to need this delay because of the call above, but I do...
		}
		[TestMethod]
		public void Remove()
		{
			var valueCollected = new booleanWrapper();
			var keyCollected = new booleanWrapper();

			var dict = new WeakReferenceDictionary<VisiblyGarbageCollectedObject, VisiblyGarbageCollectedObject>(ReferenceEqualityComparer.Instance);
			addKeyAndValueWrappersToDict();

			GC.Collect(); // collects the key
			dict.Clean();    // removes the last reference to the value
			waitForGC();

			Assert.IsTrue(keyCollected);
			Assert.IsFalse(valueCollected);

			GC.Collect(); // collects the value
			waitForGC();

			Assert.IsTrue(valueCollected);

			void addKeyAndValueWrappersToDict()
			{
				dict.Add(VisiblyGarbageCollectedObject.Create(keyCollected), VisiblyGarbageCollectedObject.Create(valueCollected));
			}
		}
	}

	class VisiblyGarbageCollectedObject
	{
		public static VisiblyGarbageCollectedObject Create(booleanWrapper flag)
		{
			return new VisiblyGarbageCollectedObject(() => flag.Value = true);
		}
		private readonly Action onGarbageCollected;
		public VisiblyGarbageCollectedObject(Action onGarbageCollected)
		{
			Contract.Requires(onGarbageCollected != null);

			this.onGarbageCollected = onGarbageCollected;
		}
		~VisiblyGarbageCollectedObject()
		{
			this.onGarbageCollected();
		}
	}

	[DebuggerDisplay("{Value}")]
	class booleanWrapper
	{
		public bool Value { get; set; }
		public static implicit operator bool(booleanWrapper wrapper) => wrapper.Value;
	}
}
