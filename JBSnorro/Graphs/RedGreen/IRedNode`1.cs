#nullable enable
namespace JBSnorro.Graphs.RedGreen;

public interface IRedNode<TSelf, TGreenNode> where TSelf : class, IRedNode<TSelf, TGreenNode>
{
    TSelf? Parent { get; }
    IReadOnlyList<TSelf> Elements { get; }
    protected internal TGreenNode Green { get; }
    protected internal int IndexInParent { get; }
    
    /// <summary>
    /// Creates a root red node.
    /// </summary>
    /// <param name="green">The green node representing the value tree of this red node.</param>
    public static virtual TSelf Create(TGreenNode green) => TSelf.Create(green, default, default);
    /// <summary>
    /// Creates a red node.
    /// </summary>
    /// <param name="green">The green node representing the value tree of this red node.</param>
    /// <param name="parent">The parent of the red node. If not null, <code>parent.Elements[indexInParent]</code> must equal <paramref name="green"/>.</param>
    /// <param name="indexInParent">The index in the parent's elements for which a red node is to be created. Must be -1 if no parent is specified. </param>
    /// <returns></returns>
    protected internal static abstract TSelf Create(TGreenNode green, TSelf? parent, int? indexInParent);
}
