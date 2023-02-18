#nullable enable

namespace JBSnorro.Graphs;

/// <summary>
/// Asnything that derives from this must be immutable.
/// </summary>
public interface IGreenNode<TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    IReadOnlyList<TGreenNode> Elements { get; }
    TGreenNode With(IReadOnlyList<TGreenNode> elements);
}
public interface IRedNode<TSelf, TGreenNode> /*: IGreenNode<TGreenNode>, because RedNode is not immutable */ where TSelf : class, IRedNode<TSelf, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    protected TGreenNode Green { get; }
    TSelf? Parent { get; }
    IReadOnlyList<TSelf> Elements { get; }


    static abstract TSelf Create(TGreenNode green, TSelf? parent);
    static virtual TSelf Create(TGreenNode value, IReadOnlyList<TGreenNode> elements, TSelf? parent) => TSelf.Create(value, parent).With(elements);
    public TSelf With(IReadOnlyList<TGreenNode> elements)
    {
        if (ReferenceEquals(elements, this.Elements))
        {
            return (TSelf)this;
        }

        var newGreen = this.Green.With(elements);
        return TSelf.Create(newGreen, parent: default); // no parent, because you can't have that the parent doesn't know about the children for red nodes.
    }
}
