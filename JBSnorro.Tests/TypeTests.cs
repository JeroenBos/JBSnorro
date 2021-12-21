using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class TypeTests
	{
		// the assertions in these tests are implicit: asserting that they don't throw
		[TestMethod]
		public void TestToActionOnParameterlessStaticMethod()
		{
			var methodInfo = typeof(TypeTests).GetMethod(nameof(dummyStaticMethod), BindingFlags.Static | BindingFlags.NonPublic);

			Action method = methodInfo.ToAction();

			method();
		}
		private static void dummyStaticMethod() { }
		private static void dummyStaticMethodWithParameter(object arg) { }
		private void dummyInstanceMethod() { }


		[TestMethod]
		public void TestToActionOnInstanceMethodWithOneParameter()
		{
			var methodInfo = typeof(TypeTests).GetMethod(nameof(dummyInstanceMethod), BindingFlags.Instance | BindingFlags.NonPublic);

			Action<TypeTests> method = methodInfo.ToAction<TypeTests>();

			method(new TypeTests());

			Assert.IsNotNull(method);
		}
		[TestMethod]
		public void TestToActionOnInstanceMethodWithOneParameterWithReturnType()
		{
			var methodInfo = typeof(object).GetMethod(nameof(object.GetType));

			Action<object> method = methodInfo.ToAction<object>();

			method(new object());

			Assert.IsNotNull(method);
		}
		[TestMethod]
		public void TestToActionOnStaticMethodWithOneParameter()
		{
			var methodInfo = typeof(TypeTests).GetMethod(nameof(dummyStaticMethodWithParameter), BindingFlags.Static | BindingFlags.NonPublic);

			Action<object> method = methodInfo.ToAction<object>();

			method(new object());

			Assert.IsNotNull(method);
		}
		[TestMethod]
		public void TestToActionOnStaticMethodWithReturnType()
		{
			var methodInfo = typeof(Math).GetMethod(nameof(Math.Sin));

			Action<double> method = methodInfo.ToAction<double>();

			method(0.1);

			Assert.IsNotNull(method);
		}
		[TestMethod]
		public void TestToFuncOnStaticMethod()
		{
			var methodInfo = typeof(Math).GetMethod(nameof(Math.Sin));

			Func<double, double> method = methodInfo.ToFunc<double, double>();

			method(0.1);

			Assert.IsNotNull(method);
		}

		private int dummyField = -1; // -1 removes warning
		[TestMethod]
		public void TestInstanceFieldToFunc()
		{
			const int expected = 2;
			var fieldInfo = typeof(TypeTests).GetField(nameof(dummyField), BindingFlags.Instance | BindingFlags.NonPublic);

			var getter = fieldInfo.ToFunc<TypeTests, int>();
			var setter = fieldInfo.ToAction<TypeTests, int>();

			var obj = new TypeTests();
			setter(obj, expected);
			int result = getter(obj);

			Assert.AreEqual(expected, result);
		}
		[TestMethod]
		public void TestEventHandlerHasHandlerSignature()
		{
			bool hasSignature = typeof(AppDomain).GetEvent(nameof(AppDomain.AssemblyLoad)).HasHandlerSignature<AssemblyLoadEventArgs>();

			Assert.IsTrue(hasSignature);
		}
		[TestMethod]
		public void TestEventHandlerHasHandlerSignatureIncovariance()
		{
			bool hasSignature = typeof(AppDomain).GetEvent(nameof(AppDomain.AssemblyLoad)).HasHandlerSignature<EventArgs>();

			Assert.IsFalse(hasSignature);
		}
		[TestMethod]
		public void TestEventHandlerHasSignature()
		{
			bool hasSignature = typeof(AppDomain).GetEvent(nameof(AppDomain.AssemblyLoad)).HasSignature<AssemblyLoadEventHandler>();

			Assert.IsTrue(hasSignature);
		}
	}
}
