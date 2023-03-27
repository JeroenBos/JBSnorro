using System.Diagnostics;
using static JBSnorro.Extensions.EnvironmentExtensions;

namespace JBSnorro.Tests;

class Program
{
    [DebuggerHidden]
    public static Task Main(string[] args)
    {
        // RunSettingsUtilities.LoadEnvironmentVariables(runSettingsXmlPath: GetRequiredEnvironmentVariable("RUNSETTINGS_PATH"));

        return Testing.TestExtensions.DefaultMainTestProjectImplementation(args);
    }
}
