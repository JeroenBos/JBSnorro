namespace JBSnorro.Testing.IntertestDependency;

/// <summary> Represents an identifier of a test or set of tests. </summary>
public interface ITestIdentifier : IEquatable<ITestIdentifier>
{
    public static ITestIdentifier From(Type testType) => TestIdentifier.From(testType);
    public static ITestIdentifier From(Type testType, string testMethodName) => TestIdentifier.From(testType, testMethodName);
    public static ITestIdentifier From(MethodInfo testInfo) => TestIdentifier.From(testInfo);
    internal static ITestIdentifier From(string identifier, Type callerType) => TestIdentifier.From(identifier, callerType);
}
