using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Testing
{
	public static class TestingExtensions
	{
		public static void InvokeAllTestMethodsSynchronouslyIn(this Type testClass)
		{
			InvokeAllTestMethodsAsynchronouslyIn(testClass, 1);
		}
		public static void InvokeAllTestMethodsAsynchronouslyIn(this Type testClass)
		{
			InvokeAllTestMethodsAsynchronouslyIn(testClass, 8);
		}
		public static void InvokeAllTestMethodsAsynchronouslyIn(this Type testClass, int maxDegreeOfParallelism)
		{

			Func<MethodInfo, bool> isTestMethodPredicate = methodInfo => methodInfo.GetParameters().Length == 0
																		 && methodInfo.GetGenericArguments().Length == 0
																		 && methodInfo.ReturnType == typeof(void)
																		 && methodInfo.GetCustomAttributes().Any(attribute => attribute.GetType().Name == "TestMethodAttribute");

			Parallel.ForEach(testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(isTestMethodPredicate), new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, // ReSharper disable once PossibleNullReferenceException
							 testMethod => testMethod.Invoke(testClass.GetConstructor(new Type[0]).Invoke(new object[0]), new object[0]));
		}
	}
}
