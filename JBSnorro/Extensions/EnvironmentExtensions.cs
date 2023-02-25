using System.Diagnostics;

namespace JBSnorro.Extensions;

public class EnvironmentExtensions
{
    /// <summary>
    /// Gets the specified environment variable. Throws if it's not found.
    /// </summary>
    /// <param name="name">The name of the environment variable to get. </param>
    /// <exception cref="EnvironmentVariableNotFoundException"></exception>
    [DebuggerHidden]
    public static string GetRequiredEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new EnvironmentVariableNotFoundException(name);
    }
    /// <summary>
    /// Gets the specified environment variable, optionally expanded values like '%USERPROFILE'. Throws if the environment variable is not found.
    /// </summary>
    /// <param name="name">The name of the environment variable to get. </param>
    /// <exception cref="EnvironmentVariableNotFoundException"></exception>
    public static string GetRequiredEnvironmentVariable(string name, bool expandContainedVariables)
    {
        var value = GetRequiredEnvironmentVariable(name);
        if (expandContainedVariables)
        {
            return Environment.ExpandEnvironmentVariables(value);
        }
        return value;
    }

    public static bool IsCI
    {
        get
        {
            return bool.Parse(Environment.GetEnvironmentVariable("CI") ?? "false");
        }
    }
}


public class EnvironmentVariableNotFoundException : Exception
{
    private const string message = "Environment variable '{0}' not found.";
    public EnvironmentVariableNotFoundException(string environmentVariableName) : base(string.Format(message, environmentVariableName)) { }
    public EnvironmentVariableNotFoundException(string environmentVariableName, Exception inner) : base(string.Format(message, environmentVariableName), inner) { }
}
