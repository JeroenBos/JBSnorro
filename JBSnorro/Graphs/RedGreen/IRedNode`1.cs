#nullable enable
namespace JBSnorro.Graphs.RedGreen;

public interface IRedNode<TSelf, TGreenNode> where TSelf : class, IRedNode<TSelf, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    TSelf? Parent { get; }
    IReadOnlyList<TSelf> Elements { get; }
    protected internal TGreenNode Green { get; }
    protected internal int IndexInParent { get; }

    static virtual TSelf Create(TGreenNode green) => TSelf.Create(green, default, default);
    protected internal static abstract TSelf Create(TGreenNode green, TSelf? parent, int? indexInParent);
}
