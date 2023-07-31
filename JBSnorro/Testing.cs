namespace JBSnorro.Testing;

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
            Parallel.ForEach(TestExtensions.GetTestMethods(testClass), 
			             new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism },
						 test => test.Method.Invoke(testClass.GetConstructor(EmptyCollection<Type>.Array)!.Invoke(EmptyCollection<object>.Array), EmptyCollection<object>.Array));
	}
}
