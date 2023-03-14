using System;
using System.Collections.Generic;
using System.Linq;
using JBSnorro.Testing.IntertestDependency.Tests;
using JBSnorro.Testing.IntertestDependency.Inference;
using JBSnorro.Csx;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace JBSnorro.Testing.IntertestDependency.Inference.Tests;

public class IntertestDependencyOrdererTests : IntertestDependencyIntegrationTestsBase
{
    protected static Task<AsyncDisposable<ProcessOutput>> RunDotnetTest(string csContents)
    {
        return IntertestDependencyIntegrationTestsBase.RunDotnetTest(csContents: csContents, csprojContents: csprojContents);
    }

    [@Fact]
    public async Task Test_can_run_dotnet_test_on_tmp_setup()
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
        Contract.Assert(stdOutput.Contains(ExpectedTally(passed: 1, skipped: 1)));
    }
}

