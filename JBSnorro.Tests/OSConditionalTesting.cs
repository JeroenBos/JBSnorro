using System.IO;
using JBSnorro;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute; // or NUnit.Framework.TestAttribute

namespace Tests.JBSnorro;

class TestOnWindowsOnly
#if WINDOWS
	: TestAttribute
#else
: Attribute
#endif
{

}
class TestOnLinuxOnly
#if LINUX
	: TestAttribute
#else
: Attribute
#endif
{

}

[TestClass]
public class DefineConstantsTests
{
#if WINDOWS
    [Test]
    public void TestWindowsOS()
    {
        Assert.IsTrue(OperatingSystem.IsWindows());
        Assert.IsFalse(OperatingSystem.IsLinux());
    }	    
#else
    [Test]
    public void TestNotWindowsOS()
    {
        Assert.IsFalse(OperatingSystem.IsWindows());
        Assert.IsTrue(OperatingSystem.IsLinux());
    }
#endif

#if LINUX
	    [Test]
	    public void TestLinuxOS()
	    {
		    Assert.IsFalse(OperatingSystem.IsWindows());
		    Assert.IsTrue(OperatingSystem.IsLinux());
	    }
#else
    [Test]
    public void TestNotLinuxOS()
    {
        Assert.IsTrue(OperatingSystem.IsWindows());
        Assert.IsFalse(OperatingSystem.IsLinux());
    }
#endif
}