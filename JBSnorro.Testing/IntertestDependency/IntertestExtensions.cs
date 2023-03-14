using JBSnorro.Graphs;

namespace JBSnorro.Testing.IntertestDependency;

public static class IntertestExtensions
{
    /// <param name="this"> The this parameter is useful three-fold: 
    /// - we can then infer the calling type
    /// - then we can infer circular dependencies
    /// - and for convenience: `await this.DependsOn(SomeTest)` reads better than `await IntertestExtensions.DependsOn(SomeTest)`. </param>
    /// <param name="name"> The name of a local test or a test type on which this depends. </param>
    public static Task DependsOn(this object @this, string name, [CallerMemberName] string calledMemberName = null!)
    {
        try
        {
            ITestIdentifier identifier = TestIdentifier.From(name, @this.GetType());
            var dependencyTracker = IIntertestDependencyTracker.GetDefault();
            if (calledMemberName == null)
            {
                return dependencyTracker.DependsOn(new[] { identifier });
            }
            else
            {
                var caller = TestIdentifier.From(calledMemberName, @this.GetType());
                return dependencyTracker.DependsOn(new[] { identifier }, caller);
            }
        }
        catch (CircularDependencyException ex)
        {
            throw new InvalidTestConfigurationException("No circular test dependencies are allowed", ex);
        }
    }
    //public static ITestIdentifier DependsOn(this object @this, string fullname, string fullname2, [CallerMemberName] string? memberName = null)
    //{
    //    string s = nameof(IIntertestDependency.DependsOn);
    //}
    //internal interface IIntertestDependency // use these methods instead
    //{
    //    Task DependsOn(params Type[] t);
    //    /// <summary>
    //    /// Asserts that the caller's dependencies have not failed yet; otherwise an Inconclusive test result is emitted.
    //    /// </summary>
    //    Task DependsOn(params Delegate[] o);
    //    Task DependsOn(params string[] t);
    //    Task DependsOn(params object[] t);
    //}
}
