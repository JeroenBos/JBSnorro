using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Csx.Node;

/// <summary>
/// This type has the responsibility of resolving the path to a node executable.
/// </summary>
public interface INodePathResolver
{
    string Path { get; }

    /// <summary>
    /// Gets a <see cref="INodePathResolver"/> that doesn't resolve the path to node, but refers to it by the command name `node`, so uses PATH.
    /// </summary>
    [DebuggerHidden]
    public static INodePathResolver FromCommand()
    {
        return NodeCommand.Instance;
    }
    /// <summary>
    /// Gets a <see cref="INodePathResolver"/> by specifying where node is.
    /// </summary>
    /// <param name="path">The path of the node executable.</param>
    /// <param name="skipVerify">Whether to skip verifying that the path can be resolved.</param>
    /// <exception cref="FileNotFoundException"></exception>
    [DebuggerHidden]
    public static INodePathResolver FromPath(string path, bool skipVerify = false)
    {
        Contract.Requires(!string.IsNullOrWhiteSpace(path));

        var expandedPath = Environment.ExpandEnvironmentVariables(path);
        if (!skipVerify && !File.Exists(expandedPath))
        {
            throw new FileNotFoundException($"Resolving node failed. Not found at '{expandedPath}'", fileName: expandedPath);
        }

        return new NodePathResolverFromPath { Path = expandedPath };
    }
}



file class NodeCommand : INodePathResolver
{
    public static readonly NodeCommand Instance = new();
    public string Path => "node";
    private NodeCommand() { }
}


file class NodePathResolverFromPath : INodePathResolver
{
    public required string Path { get; init; }
}
