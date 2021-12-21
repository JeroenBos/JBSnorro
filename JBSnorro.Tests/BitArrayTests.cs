using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class BitArrayTests
	{
		[TestMethod]
		public void BitArrayCtorTest()
		{
			BitArray testObject = new BitArray(new bool[] { true, false, true, true, false, false, false, true });

			Contract.Assert(testObject.Length == 8);
			Contract.Assert(testObject[0]);
			Contract.Assert(!testObject[1]);
			Contract.Assert(testObject[2]);
			Contract.Assert(testObject[3]);
			Contract.Assert(!testObject[4]);
			Contract.Assert(!testObject[5]);
			Contract.Assert(!testObject[6]);
			Contract.Assert(testObject[7]);
		}
	}
}
