namespace JBSnorro.Testing.IntertestDependency;

public interface IIntertestDependencyTracker
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    /// <summary>
    /// Allows to configure how which IIntertestDependencyTracker is used for all tests.
    /// </summary>
    public static Func<IIntertestDependencyTracker> GetSingleton = () => IntertestDependencyTracker.singleton;
    /// <summary>
    /// The list of assemblies that can be searched through for types containing tests.
    /// </summary>
    public static IEnumerable<Assembly>? TestAssemblies;  // TODO: find a better solution
#pragma warning restore CA2211 // Non-constant fields should not be visible

    public static IIntertestDependencyTracker Singleton => GetSingleton();

    /// <summary>
    /// Raises a <see cref="SkipException"/> if any of the dependency tests failed.
    /// </summary>
    /// <param name="testsIdentifiers"> The identifiers of the tests the current test depends on.</param>
    Task DependsOn(ITestIdentifier[] testsIdentifiers);
    /// <summary>
    /// Raises a <see cref="SkipException"/> if any of the dependency tests failed, and checks for circular dependencies.
    /// </summary>
    /// <param name="testsIdentifiers"> The identifiers of the tests the current test depends on.</param>
    /// <param name="current">The identifier of the current test.</param>
    Task DependsOn(ITestIdentifier[] testsIdentifiers, ITestIdentifier current)
    {
        FindCircularDependencies(current, testsIdentifiers);
        return DependsOn(testsIdentifiers);
    }
    /// <summary>
    /// Throws if adding the test with its dependencies would cause a circular dependency chain.
    /// </summary>
    void FindCircularDependencies(ITestIdentifier test, ITestIdentifier[] dependencies);
}
