using Microsoft.VisualStudio.TestTools.UnitTesting;

public class TestMethodWithRetryAttribute : TestMethodAttribute
{
    public required int Count { get; init; }

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        var count = Count;
        TestResult[]? result = null;
        while (count > 0)
        {
            try
            {
                result = base.Execute(testMethod);
                if (result[0].TestFailureException != null)
                {
                    throw result[0].TestFailureException!;
                }
            }
            catch (Exception) when (count > 0)
            {
            }
            finally
            {
                count--;
            }
        }
        return result!;
    }

}