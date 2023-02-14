#nullable enable
using JBSnorro.Graphs;

namespace JBSnorro.Tests.Graphs;

public class TrivialRedNode : IRedNode<TrivialRedNode, TrivialGreenNode>
{
    public TrivialRedNode? Parent { get; }

    public IReadOnlyList<TrivialGreenNode> Elements { get; }

    public TrivialRedNode(IReadOnlyList<TrivialGreenNode> elements)
    {
        Elements = elements;
    }
    public TrivialRedNode(TrivialRedNode parent, IReadOnlyList<TrivialGreenNode> elements)
    {
        Parent = parent;
        Elements = elements;
    }

    public static TrivialRedNode Create(TrivialGreenNode green)
    {
        return new TrivialRedNode(green.Elements);
    }

    public TrivialGreenNode With(IReadOnlyList<TrivialGreenNode> elements)
    {
        return new TrivialGreenNode(elements);
    }
}
