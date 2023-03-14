#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JBSnorro.Testing.IntertestDependency.Tests;
using JBSnorro.Testing.IntertestDependency.Inference;
using JBSnorro.Csx;
using Xunit.Abstractions;
using Xunit.Sdk;
using Xunit;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Testing.IntertestDependency.Inference.Tests;

public class IntertestDependencyOrdererTests : IntertestDependencyIntegrationTestsBase
{
    protected static Task<AsyncDisposable<ProcessOutput>> RunDotnetTest(string csContents)
    {
        return IntertestDependencyIntegrationTestsBase.RunDotnetTest(csContents: csContents, csprojContents: csprojContents);
    }

    private static TestMethod CreateStubTestMethod(string testName, params IAttributeInfo[] dependsOnCustomAttributes)
    {
        var testCollection = new TestCollection(new TestAssembly(), default, testName);
        var typeInfo = new ReflectionTypeInfo(typeof(IntertestDependencyOrdererTests));
        var testClass = new TestClass(testCollection, typeInfo);
        var methodInfo = new MethodInfoStub(dependsOnCustomAttributes);
        return new TestMethod(testClass, methodInfo);
    }

    private static TestMethod CreateStubTestMethod(string testName, params object[][] dependsOnCustomAttributes)
    {
        var attributeInfos = dependsOnCustomAttributes.Select(constructorArguments => new AttributeInfoStub() { ConstructorArguments = constructorArguments })
                                                      .ToArray();
        return CreateStubTestMethod(testName, attributeInfos);
    }
    private static ITestCase CreateStubTestCase(string testName, params object[][] dependsOnCustomAttributes)
    {
        var testMethod = CreateStubTestMethod(testName, dependsOnCustomAttributes);
        return new XunitTestCase(default, default, default, testMethod);
    }

    private readonly ITestCaseOrderer testCaseOrderer = IntertestDependency.Inference.TestcaseDependencyOrderer.Instance;


    [@Fact]
    public void Test_dependency_test_is_before_dependant_test()
    {
        var testCase1 = CreateStubTestCase("Test1 (Depends on Test2)", dependsOnCustomAttributes: new[] { new object[] { "" } });
        var testCase2 = CreateStubTestCase("Test2");
        var resultingTestcases = testCaseOrderer.OrderTestCases(new[] { testCase2, testCase1 }).ToList();

        Contract.AssertSequenceEqual(new[] { testCase1, testCase2 }, resultingTestcases);
    }


    [@Fact]
    public async Task Test_depends_on_attribute_skips_because_dependency_fails()
    {
        // Arrange
        string csContents = """
            using System;
            using System.Threading.Tasks;
            using Xunit;
            using JBSnorro.Testing.IntertestDependency;
            
            public class UnitTest1
            {
                [Fact, DependsOn(nameof(TestMethod2))]
                public void TestMethod1()
                {
                    
                }
                [Fact]
                public void TestMethod2()
                {
                    throw new Exception();
                }
            }
            """;

        // Act
        await using var dotnetTestOutput = await RunDotnetTest(csContents);
        var (exitCode, stdOutput, errorOutput) = dotnetTestOutput.Value;

        Contract.Assert(exitCode == 0, stdOutput);
        Contract.Assert(stdOutput.Contains(TestsStartedExpected));
        Contract.Assert(stdOutput.Contains(TestsStartedExpected2));
        Contract.Assert(stdOutput.Contains(ExpectedTally(failed: 1, skipped: 1)));
    }
}
internal class AttributeInfoStub : IAttributeInfo
{
    public IEnumerable<object> ConstructorArguments { get; init; } = JBSnorro16.EnumerableExtensions_Throw<object>();
    IEnumerable<object> IAttributeInfo.GetConstructorArguments() => ConstructorArguments;
    IEnumerable<IAttributeInfo> IAttributeInfo.GetCustomAttributes(string assemblyQualifiedAttributeTypeName) => throw new NotImplementedException();
    TValue IAttributeInfo.GetNamedArgument<TValue>(string argumentName) => throw new NotImplementedException();
}

internal class MethodInfoStub : Xunit.Abstractions.IMethodInfo
{
    public MethodInfoStub()
    {
        CustomAttributesPerQualifiedAttributeTypeName = new Dictionary<string, IEnumerable<IAttributeInfo>>();
    }
    public MethodInfoStub(IEnumerable<IAttributeInfo> dependsOnCustomAttributes)
    {
        CustomAttributesPerQualifiedAttributeTypeName = new Dictionary<string, IEnumerable<IAttributeInfo>>()
        {
            {typeof(DependsOnAttribute).AssemblyQualifiedName!, dependsOnCustomAttributes }
        };
    }
    public bool IsAbstract { get; init; }
    public bool IsGenericMethodDefinition { get; init; }
    public bool IsPublic { get; init; }
    public bool IsStatic { get; init; }
    public string? Name { get; init; }
    public ITypeInfo? ReturnType { get; init; }
    public ITypeInfo? Type { get; init; }
    public IReadOnlyDictionary<string /*assemblyQualifiedAttributeTypeName*/, IEnumerable<IAttributeInfo>> CustomAttributesPerQualifiedAttributeTypeName { get; init; }
    public IEnumerable<ITypeInfo> GenericArguments { get; init; } = JBSnorro16.EnumerableExtensions_Throw<ITypeInfo>();
    public IEnumerable<IParameterInfo> Parameters { get; init; } = JBSnorro16.EnumerableExtensions_Throw<IParameterInfo>();



    public IMethodInfo MakeGenericMethod(params ITypeInfo[] typeArguments) => throw new NotImplementedException();
    [DebuggerHidden] IEnumerable<ITypeInfo> IMethodInfo.GetGenericArguments() => GenericArguments;
    [DebuggerHidden] IEnumerable<IParameterInfo> IMethodInfo.GetParameters() => Parameters;
    [DebuggerHidden] IEnumerable<IAttributeInfo> IMethodInfo.GetCustomAttributes(string assemblyQualifiedAttributeTypeName) => CustomAttributesPerQualifiedAttributeTypeName[assemblyQualifiedAttributeTypeName];

}
internal class ITestCaseSub : LongLivedMarshalByRefObject, ITestCase
{
    //XunitTestCase
    public string? DisplayName => throw new NotImplementedException();
    public string? SkipReason => throw new NotImplementedException();
    public ISourceInformation? SourceInformation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ITestMethod? TestMethod => throw new NotImplementedException();
    public object[]? TestMethodArguments => throw new NotImplementedException();
    public Dictionary<string, List<string>>? Traits => throw new NotImplementedException();
    public string? UniqueID => throw new NotImplementedException();
    public void Deserialize(IXunitSerializationInfo info) => throw new NotImplementedException();
    public void Serialize(IXunitSerializationInfo info) => throw new NotImplementedException();
}