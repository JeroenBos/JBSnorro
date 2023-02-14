#nullable enable

namespace JBSnorro.Graphs;

public interface IGreenNode<TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    IReadOnlyList<TGreenNode> Elements { get; }
    TGreenNode With(IReadOnlyList<TGreenNode> elements);
}
public interface IRedNode<TSelf, TGreenNode> : IGreenNode<TGreenNode> where TSelf : class, IRedNode<TSelf, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    TSelf? Parent { get; }
    TSelf this[int index] => TSelf.Create(this.Elements[index]);

    static abstract TSelf Create(TGreenNode green);
    static virtual TSelf Create(TGreenNode value, IReadOnlyList<TGreenNode> elements) => TSelf.Create(value).With(elements);
    new TSelf With(IReadOnlyList<TGreenNode> elements)
    {
        IGreenNode<TGreenNode> greenSelf = this;
        var newGreen = greenSelf.With(elements);
        return TSelf.Create(newGreen);
    }
}
