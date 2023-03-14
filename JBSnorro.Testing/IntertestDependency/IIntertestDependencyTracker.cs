namespace JBSnorro.Testing.IntertestDependency;

public interface IIntertestDependencyTracker
{
    /// <summary>
    /// Gets the default <see cref="IIntertestDependencyTracker"/>.
    /// </summary>
    public static IIntertestDependencyTracker Default => GetDefault();
    /// <summary>
    /// Allows to configure the default <see cref="IIntertestDependencyTracker"/> used in all tests.
    /// </summary>
    public static Func<IIntertestDependencyTracker> GetDefault { get; set; } = () => IntertestDependencyTracker.singleton;
    /// <summary>
    /// The list of assemblies that can be searched through for types containing tests.
    /// Defaults to the assembly containing the type depended on.
    /// </summary>
    public static IEnumerable<Assembly>? TestAssemblies { get; set; }
    

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
