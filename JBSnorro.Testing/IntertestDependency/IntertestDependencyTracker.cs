using System.Collections.Immutable;
using Xunit;
using JBSnorro.Algorithms;
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using TypeExtensions = JBSnorro.Extensions.TypeExtensions;

namespace JBSnorro.Testing.IntertestDependency;

internal class IntertestDependencyTracker : IIntertestDependencyTracker
{
    public static readonly IntertestDependencyTracker singleton = new();
    public IImmutableDictionary<TestIdentifier, ITestRun> TestResults => testResults;


    private ImmutableDictionary<TestIdentifier, ITestRun> testResults;
    private readonly CircularDependencyTracker<TestIdentifier> circularReferenceTracker;

    private IntertestDependencyTracker()
    {
        this.testResults = ImmutableDictionary<TestIdentifier, ITestRun>.Empty;
        this.circularReferenceTracker = new CircularDependencyTracker<TestIdentifier>(TestIdentifier.TestIdentifierContainmentEqualityComparerInstance);
    }

    /// <inheritdoc cref="IIntertestDependencyTracker.DependsOn(ITestIdentifier[])"/>
    public async Task DependsOn(params TestIdentifier[] testsIdentifiers)
    {
        Contract.Requires(testsIdentifiers != null);
        Contract.Requires(Contract.ForAll(testsIdentifiers, _ => _ != null));
        Contract.Requires(Enumerable.Distinct(testsIdentifiers, testResults.KeyComparer).Count() == testsIdentifiers.Length);

        // could be parallelized, but should respect the test runner's parallelization
        // make sure that any InconclusiveTestException is propagated up here. All other exceptions should probably be converted?
        foreach (var test in testsIdentifiers)
        {
            try
            {
                await this.DependsOn(test);
            }
            catch (SkipException)
            {
                throw;
            }
            catch (InvalidTestConfigurationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SkipException("Skipping because a dependency test failed", ex);
            }
        }
    }
    /// <inheritdoc cref="IIntertestDependencyTracker.FindCircularDependencies(ITestIdentifier, ITestIdentifier[])"/>
    public void FindCircularDependencies(TestIdentifier node, TestIdentifier[] dependencies)
    {
        this.circularReferenceTracker.Add(node, dependencies);
    }


    private async Task DependsOn(TestIdentifier testIdentifier)
    {
        var created = new TestRun(testIdentifier);
        var testRunInDict = ImmutableInterlocked.GetOrAdd<TestIdentifier, ITestRun>(ref testResults, testIdentifier, created);
        if (ReferenceEquals(created, testRunInDict))
        {
            // it was newly created
            // shall we kick off the test, or wait for its accidental completion?
            //
            // OPEN QUESTION: a test that has no call to this machinery, how can it ever be depended on to completion? Probably we have to kick it off...
            // Ideally the test discoverer is aware right to prevent duplicate testing.
            // 
            //
            //
            // The simplest thing to do (with which we'll start) is just to return the task that would run the test, and, skip if appropriate

            created.TestTask.Start();
            await await created.TestTask;
        }
        else
        {
            await testRunInDict.TestTask;
            Skip.If(testRunInDict.CompletedUnsuccessfully);
        }
    }
    Task IIntertestDependencyTracker.DependsOn(ITestIdentifier[] testsIdentifiers) => this.DependsOn(testsIdentifiers.Cast<TestIdentifier>().ToArray());
    void IIntertestDependencyTracker.FindCircularDependencies(ITestIdentifier node, ITestIdentifier[] dependencies) => FindCircularDependencies((TestIdentifier)node, dependencies.Cast<TestIdentifier>().ToArray());


    private sealed class TestRun : ITestRun
    {
        public bool CompletedUnsuccessfully { get; private set; }
        public bool CompletedSuccessfully { get; private set; }
        public bool Pending { get; private set; }
        public Task<Task> TestTask { get; init; } = default!;


        /// <summary>
        /// Creates a TestResult of a test that's still to be started.
        /// </summary>
        /// <returns> a task representing the test. </returns>
        public TestRun(TestIdentifier testIdentifier)
        {
            this.TestTask = new Task<Task>(() => this.RunTask(testIdentifier));
        }
        private async Task RunTask(TestIdentifier testIdentifier)
        {
            this.Pending = true;
            try
            {
                var testType = Type.GetType(testIdentifier.TypeName);
                if (testType == null)
                {
                    testType = TypeExtensions.FindType(testIdentifier.TypeName)
                                             .Where(t => t.IsPublic)
                                             .FirstOrDefault();
                    if (testType == null)
                    {
                        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                        var correctAssembly = allAssemblies.Where(a => a.GetName().Name == "tests").First();
                        var correctType = correctAssembly.GetTypes().Where(t => t.Name == testIdentifier.TypeName).ToList();
                        throw new InvalidTestConfigurationException($"The type '{testIdentifier.TypeName}' could not be found");
                    }
                }

                IEnumerable<Func<Task>> tests = testType.GetExecutableTestMethods($"{testIdentifier.TypeName}::{(testIdentifier.IsType ? "*" : testIdentifier.TestName)}");

                if (EnumerableExtensions.IsEmpty(ref tests))
                {
                    throw new InvalidTestConfigurationException($"The test identifier '{testIdentifier}' does not refer to any tests");
                }

                foreach (var test in tests)
                {
                    await test();
                }
            }
            catch
            {
                this.CompletedUnsuccessfully = true;
                this.Pending = false;
                throw;
            }
            this.CompletedSuccessfully = true;
            this.Pending = false;
        }
    }
}
