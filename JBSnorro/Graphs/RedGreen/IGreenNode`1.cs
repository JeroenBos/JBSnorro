namespace JBSnorro.Graphs.RedGreen;

/// <summary>
/// Anything that derives from this must be immutable.
/// </summary>
public interface IGreenNode<TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    IReadOnlyList<TGreenNode> Elements { get; }
    TGreenNode With(IReadOnlyList<TGreenNode> elements);
}
