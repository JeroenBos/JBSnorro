using System.Diagnostics;

namespace JBSnorro.Tests;

class Program
{
    [DebuggerHidden]
    public static Task Main(string[] args) => Testing.TestExtensions.DefaultMainTestProjectImplementation(args);
}
