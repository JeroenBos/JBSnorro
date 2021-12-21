using JBSnorro.Diagnostics;
using JBSnorro.Dynamic;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class NonPublicMembersDynamicWrapperTests
	{
		[TestMethod]
		public void Test()
		{
			Console.WriteLine(System.Environment.Version);
			dynamic a = new NonPublicMembersDynamicWrapper(new A());
			int i = (int)a.i;
			Contract.Assert(i == 5);
		}
		// [TestMethod]
		public void TestIndexer()
		{
			dynamic a = new NonPublicMembersDynamicWrapper(new A());
			int i = (int)a[0];
			Contract.Assert(i == 5);
		}

		// [TestMethod]
		public void TestStatic()
		{
			dynamic a = new StaticMembersDynamicWrapper(typeof(B));
			int i = (int)a.b;
			Contract.Assert(i == 6);
		}
		
	}
	class A
	{
		private int this[int i] => 5;
#pragma warning disable CS0169, CS0414
		private int i = 5;
#pragma warning restore CS0169, CS0414
	}
	class B
	{
		public static int b = 6;
	}
}
