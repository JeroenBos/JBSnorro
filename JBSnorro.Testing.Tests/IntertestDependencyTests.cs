using System.Diagnostics;

namespace JBSnorro.Testing.IntertestDependency.Tests;

public class IntertestDependencyTests
{
    [@Fact]
    public async Task Test_depends_on()
    {
        await this.DependsOn(nameof(EmptyTest));
    }
    [@Fact]
    public void EmptyTest()
    {
    }
    [@Fact]
    public async Task Test_depends_on_not_depending_on_a_test_throws()
    {
        await Assert.ThrowsAsync<InvalidTestConfigurationException>(() => this.DependsOn(nameof(NotATest)));
    }
    [@Fact]
    public async Task Test_depending_on_self_throws()
    {
        await Assert.ThrowsAsync<InvalidTestConfigurationException>(() => this.DependsOn(nameof(Test_depending_on_self_throws)));
    }
    [@Fact]
    public async Task Test_locating_type_by_Name()
    {
        IIntertestDependencyTracker.TestAssemblies = new[] { typeof(IntertestDependencyTests).Assembly };

        await this.DependsOn(nameof(EmptyTestClass));
    }
    [@Fact]
    public async Task Test_depending_on_nontest_class_fails()
    {
        IIntertestDependencyTracker.TestAssemblies = new[] { typeof(IntertestDependencyTests).Assembly };

        await Assert.ThrowsAsync<InvalidTestConfigurationException>(() => this.DependsOn(nameof(IntertestDependencyIntegrationTestsBase)));
    }

#pragma warning disable xUnit1013, CA1822
    public void NotATest() { }
#pragma warning restore xUnit1013, CA1822 // Public method should be marked as test

}
public class EmptyTestClass
{
    [@Fact]
    public void EmptyTest()
    {
    }
}
public class IntertestDependencyIntegrationTestsBase
{
    protected static string TestsStartedExpected = "Starting test execution, please wait...";
    protected static string TestsStartedExpected2 = "A total of 1 test files matched the specified pattern.";
    protected static string ExpectedTally(int passed = 0, int skipped = 0, int failed = 0)
    {
        return $"{(failed == 0 ? "Passed" : "Failed")}!  - Failed:     {failed}, Passed:     {passed}, Skipped:     {skipped}";
    }

}
public class IntertestXunitDependencyIntegrationTests : IntertestDependencyIntegrationTestsBase
{
    public IntertestXunitDependencyIntegrationTests()
    {

    }
    private static string csprojContents
    {
        get
        {
            return """
              <Project Sdk="" Microsoft.NET.Sdk.Razor"">
                  <PropertyGroup>
                      <TargetFramework>net7.0</TargetFramework>
                      <Nullable>enable</Nullable>
                      <IsPackable>false</IsPackable>
                      <IsTestProject>true</IsTestProject>
                  </PropertyGroup>
                  
                  <ItemGroup>
                      <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
                      <PackageReference Include="xunit" Version="2.4.1" />
                      <PackageReference Include="xunit.runner.visualstudio" Version="2.4.3">
                          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                          <PrivateAssets>all</PrivateAssets>
                      </PackageReference>
                      <PackageReference Include="Xunit.SkippableFact" Version="1.4.13" />
                  </ItemGroup>
                  
                  <ItemGroup>
                      <PackageReference Include="JBSnorro" Version="0.0.15" />
                  </ItemGroup>
              </Project>
              """;
        }

    }


    [@Fact]
    public async Task Test_can_run_dotnet_test_on_tmp_setup()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

        }
    }
}");

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""), cts.Token);

        // Assert
        Contract.Assert(output.ExitCode == 0, output.ErrorOutput);
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(passed: 1), output.StandardOutput);
    }

    [@Fact]
    public async Task Test_can_compile_with_DependsOn()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(TestMethod2));
        }
        [Fact]
        public void TestMethod2()
        {
        }
    }
}
");

        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Contract.Assert(output.ExitCode == 0, output.ErrorOutput);
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(passed: 2), output.StandardOutput);

    }

    [@Fact]
    public async Task Test_depending_on_failing_test_raises_skip_exception()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public async Task TestMethod1()
        {
            await Assert.ThrowsAsync<SkipException>(async () => await this.DependsOn(nameof(TestMethod2)));
        }
        [Fact]
        public void TestMethod2()
        {
            throw new Exception();
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(passed: 1, failed: 1), output.StandardOutput);
    }

    [@Fact]
    public async Task Test_skip_when_dependency_test_fails()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(TestMethod2));
        }
        [Fact]
        public void TestMethod2()
        {
            throw new Exception();
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(failed: 1, skipped: 1), output.StandardOutput);

    }


    [@Fact]
    public async Task Test_does_not_stackoverflow_DependsOn_self()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(TestMethod1));
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(failed: 1), output.StandardOutput);

    }


    [@Fact]
    public async Task Test_depends_circularly_on_Type_throws()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        // public UnitTest1() { IIntertestDependencyTracker.TestAssemblies = new [] { typeof(UnitTest1).Assembly }; }

        [SkippableFact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(UnitTest1));
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(failed: 1), output.StandardOutput);
        Assert.Contains("JBSnorro.Testing.IntertestDependency.InvalidTestConfigurationException", output.StandardOutput);
        Assert.Contains("'TestProject1.UnitTest1.TestMethod1' is already depended on (indirectly) by TestProject1.UnitTest1", output.StandardOutput);

    }


    [@Fact]
    public async Task Test_depending_on_failing_test_type_skips()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(UnitTest2));
        }
    }
    public class UnitTest2
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            throw new Exception();
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(failed: 1, skipped: 1), output.StandardOutput);
    }

    [@Fact]
    public async Task Test_depending_on_asynchronously_failing_test_type_skips()
    {
        // Arrange
        string tmpDir = IOExtensions.CreateTemporaryDirectory().Value;
        File.WriteAllText(Path.Combine(tmpDir, "tests.csproj"), csprojContents);
        File.WriteAllText(Path.Combine(tmpDir, "tests.cs"),
            @"
using System;
using System.Threading.Tasks;
using Xunit;
using JBSnorro.Testing.IntertestDependency;

namespace TestProject1
{
    public class UnitTest1
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            await this.DependsOn(nameof(UnitTest2));
        }
    }
    public class UnitTest2
    {
        [SkippableFact]
        public async Task TestMethod1()
        {
            await Task.Delay(10);
            throw new Exception();
        }
    }
}
");
        // Act
        var output = await ProcessExtensions.WaitForExitAndReadOutputAsync(new ProcessStartInfo("dotnet", $"test \"{tmpDir}/tests.csproj\""));

        // Assert
        Assert.Contains(TestsStartedExpected, output.StandardOutput);
        Assert.Contains(TestsStartedExpected2, output.StandardOutput);
        Assert.Contains(ExpectedTally(failed: 1, skipped: 1), output.StandardOutput);
    }
}

