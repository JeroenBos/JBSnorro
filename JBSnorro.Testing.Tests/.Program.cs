using System.Diagnostics;

namespace JBSnorro.Testing.Tests;

class Program
{
    [DebuggerHidden]
    public static Task Main(string[] args)
    {
        return Testing.TestExtensions.DefaultMainTestProjectImplementation(args);
    }
}
