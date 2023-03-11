#nullable enable
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.Custom;

public sealed class ParallelTestFramework : XunitTestFramework
{
    public ParallelTestFramework(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink)
    {

    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
    {
        return new CustomTestExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    
    private sealed class CustomTestExecutor : XunitTestFrameworkExecutor
    {
        private static readonly PropertyInfo TestMethodTestCase_DefaultMethodDisplay = typeof(TestMethodTestCase).GetProperty("DefaultMethodDisplay", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(nameof(TestMethodTestCase_DefaultMethodDisplay));
        private static readonly PropertyInfo TestMethodTestCase_DefaultMethodDisplayOptions = typeof(TestMethodTestCase).GetProperty("DefaultMethodDisplayOptions", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(nameof(TestMethodTestCase_DefaultMethodDisplayOptions));

        public CustomTestExecutor(AssemblyName assemblyName,
                                  ISourceInformationProvider sourceInformationProvider, 
                                  IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, 
                                                   IMessageSink executionMessageSink,
                                                   ITestFrameworkExecutionOptions executionOptions)
        {
            var newTestCases = SetUpTestCaseParallelization(testCases);

            using var assemblyRunner = new XunitTestAssemblyRunner(TestAssembly, newTestCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            await assemblyRunner.RunAsync();
        }

        /// <summary>
        /// By default, all test cases in a test class share the same collection instance which ensures they run synchronously.
        /// By providing a unique test collection instance to every test case in a test class you can make them all run in parallel.
        /// </summary>
        private IEnumerable<IXunitTestCase> SetUpTestCaseParallelization(IEnumerable<IXunitTestCase> testCases)
        {
            var result = new List<IXunitTestCase>();
            foreach (var testCase in testCases)
            {
                var oldTestMethod = testCase.TestMethod;
                var oldTestClass = oldTestMethod.TestClass;
                var oldTestCollection = oldTestMethod.TestClass.TestCollection;

                // If the collection is explicitly set, don't try to parallelize test execution
                if (oldTestCollection.CollectionDefinition != null || oldTestClass.Class.GetCustomAttributes(typeof(CollectionAttribute)).Any())
                {
                    result.Add(testCase);
                    continue;
                }

                // Create a new collection with a unique id for the test case.
                var newTestCollection = new TestCollection(oldTestCollection.TestAssembly,
                                                           oldTestCollection.CollectionDefinition,
                                                           displayName: $"{oldTestCollection.DisplayName} {oldTestCollection.UniqueID}");
                newTestCollection.UniqueID = Guid.NewGuid();

                // Duplicate the test and assign it to the new collection
                var newTestClass = new TestClass(newTestCollection, oldTestClass.Class);
                var newTestMethod = new TestMethod(newTestClass, oldTestMethod.Method);
                switch (testCase)
                {
                    // Used by Theory having DisableDiscoveryEnumeration or non-serializable data
                    case XunitTheoryTestCase xunitTheoryTestCase:
                        result.Add(new XunitTheoryTestCase(DiagnosticMessageSink,
                                                           GetTestMethodDisplay(xunitTheoryTestCase),
                                                           GetTestMethodDisplayOptions(xunitTheoryTestCase),
                                                           newTestMethod));
                        break;

                    // Used by all other tests
                    case XunitTestCase xunitTestCase:
                        result.Add(new XunitTestCase(
                            DiagnosticMessageSink,
                            GetTestMethodDisplay(xunitTestCase),
                            GetTestMethodDisplayOptions(xunitTestCase),
                            newTestMethod,
                            xunitTestCase.TestMethodArguments));
                        break;

                    // TODO If you use custom attribute, you may need to add cases here

                    default:
                        throw new ArgumentOutOfRangeException("Test case " + testCase.GetType() + " not supported");
                }
            }

            return result;

            static TestMethodDisplay GetTestMethodDisplay(TestMethodTestCase testCase)
            {
                return (TestMethodDisplay)TestMethodTestCase_DefaultMethodDisplay.GetValue(testCase)!;
            }

            static TestMethodDisplayOptions GetTestMethodDisplayOptions(TestMethodTestCase testCase)
            {
                return (TestMethodDisplayOptions)TestMethodTestCase_DefaultMethodDisplayOptions.GetValue(testCase)!;
            }
        }
    }
}