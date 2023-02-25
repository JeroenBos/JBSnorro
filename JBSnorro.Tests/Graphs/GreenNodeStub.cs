using JBSnorro.Graphs.RedGreen;

namespace JBSnorro.Tests.Graphs;


public class TrivialGreenNode : IGreenNode<TrivialGreenNode>
{
    public IReadOnlyList<TrivialGreenNode> Elements { get; }

    public TrivialGreenNode(IReadOnlyList<TrivialGreenNode> elements)
    {
        Elements = elements;
    }

    public TrivialGreenNode With(IReadOnlyList<TrivialGreenNode> elements)
    {
        return new TrivialGreenNode(elements);
    }
}
