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
    protected internal TGreenNode Green { get; }
    TSelf? Parent { get; }
    IReadOnlyList<TSelf> Elements { get; }
    protected internal int IndexInParent { get; }

    static virtual TSelf Create(TGreenNode green) => TSelf.Create(green, default, default);
    protected internal static abstract TSelf Create(TGreenNode green, TSelf? parent, int? indexInParent);
    // the green node sort of defines the elements of the red node, no? 
    // having a red node copy from a green node with different elements is a bit weird? I don't see the use-case. The green node, and its structure, must be created before the red node can be created. Always

    //static virtual TSelf Create(TGreenNode value, IReadOnlyList<TGreenNode> elements, TSelf? parent) => TSelf.Create(value, parent).With(elements);
    //public TSelf With(IReadOnlyList<TGreenNode> elements)
    //{
    //    if (ReferenceEquals(elements, this.Elements))
    //    {
    //        return (TSelf)this;
    //    }

    //    var newGreen = this.Green.With(elements);
    //    return TSelf.Create(newGreen, parent: default); // no parent, because you can't have that the parent doesn't know about the children for red nodes.
    //}
}
