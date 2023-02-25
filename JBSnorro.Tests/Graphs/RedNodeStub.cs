#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Graphs.RedGreen;
using System.Xml.Linq;

namespace JBSnorro.Tests.Graphs;

public class TrivialRedNode : IRedNode<TrivialRedNode, TrivialGreenNode>
{
    public TrivialRedNode? Parent { get; }
    private readonly int indexInParent;
    public IReadOnlyList<TrivialRedNode> Elements { get; }
    public TrivialGreenNode Green { get; }



    private TrivialRedNode(TrivialGreenNode green, TrivialRedNode? parent, int indexInParent)
    {
        Contract.Requires(parent is null == indexInParent < 0);

        this.Parent = parent;
        this.indexInParent = indexInParent;
        this.Green = green;
        this.Elements = green.Elements.Map((green, i) => TrivialRedNode.Create(this, i, green));
    }

    public static TrivialRedNode Create(TrivialGreenNode green)
    {
        return new TrivialRedNode(green, null, -1);
    }
    public static TrivialRedNode Create(TrivialRedNode parent, int indexInParent, TrivialGreenNode green)
    {
        return new TrivialRedNode(green, parent, indexInParent);
    }

    public TrivialGreenNode With(IReadOnlyList<TrivialGreenNode> elements)
    {
        return new TrivialGreenNode(elements);
    }

    static TrivialRedNode IRedNode<TrivialRedNode, TrivialGreenNode>.Create(TrivialGreenNode green, TrivialRedNode? parent, int? indexInParent)
    {
        throw new NotImplementedException();
    }
    int IRedNode<TrivialRedNode, TrivialGreenNode>.IndexInParent => indexInParent;
}
