using JBSnorro.Extensions;
using System.Diagnostics;
using System.IO;
using static JBSnorro.Extensions.EnvironmentExtensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JBSnorro.Tests;

class Program
{
    //[DebuggerHidden]
    public static async Task Main(string[] args)
    {
        await foreach (var line in FileExtensions.ReadAllLinesChunkedContinuously(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop\\output.txt")))
        {
            Console.Write(".");
        }
    }
}
