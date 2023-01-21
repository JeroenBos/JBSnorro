# nullable enable

using System.Collections.Immutable;
using System.Xml;
using JBSnorro;

namespace JBSnorro.Tests;

internal static class RunSettingsUtilities
{
    /// <summary>
    /// Gets the set of user defined environment variables from a runsettings xml path as key value pairs.
    /// </summary>
    /// <param name="runSettingsXmlPath">The runsettings xml path.</param>
    /// <remarks>If there is no environment variables section defined in the settingsxml a blank dictionary is returned.</remarks>
    internal static IReadOnlyDictionary<string, string> GetTestRunEnvironmentVariables(string runSettingsXmlPath)
    {
        var xml = new XmlDocument();
        xml.Load(runSettingsXmlPath);

        return GetTestRunEnvironmentVariables(xml);
    }
    /// <summary>
    /// Gets the set of user defined environment variables from a runsettings xml as key value pairs.
    /// </summary>
    /// <param name="settingsXmlPath">The runsettings xml.</param>
    /// <remarks>If there is no environment variables section defined in the settingsxml a blank dictionary is returned.</remarks>
    internal static IReadOnlyDictionary<string, string> GetTestRunEnvironmentVariables(XmlDocument runSettingsXml)
    {
        return runSettingsXml.SelectSingleNode("RunSettings/RunConfiguration/EnvironmentVariables") switch
        {
            null => ImmutableDictionary<string, string>.Empty,
            XmlNode node => node.ChildNodes.ToDictionary(),
        };
    }
    public static IReadOnlyDictionary<string, string> ToDictionary(this XmlNodeList xmlNodes)
    {
        return xmlNodes.Cast<XmlElement>()
                       .ToDictionary(child => child.Name, child => child.InnerText);
    }
    /// <summary>
    /// Loads the specified dictionary into the current set of environment variables.
    /// </summary>
    /// <param name="values">The key value pairs to load as environment variables.</param>
    public static void LoadEnvironmentVariables(IReadOnlyDictionary<string, string> values)
    {
        foreach (var (key, value) in values)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
    /// <summary>
    /// Reads the environment variables section of a runsettings xml file and loads each into the current set of environment variables.
    /// </summary>
    /// <param name="runSettingsXmlPath">The runsettings xml path.</param>
    public static void LoadEnvironmentVariables(string runSettingsXmlPath)
    {
        IReadOnlyDictionary<string, string> envVars = GetTestRunEnvironmentVariables(runSettingsXmlPath);
        LoadEnvironmentVariables(envVars);
    }
}
