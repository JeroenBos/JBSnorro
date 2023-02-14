#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Graphs;

[TestCategory("RedGreen")]
public abstract class IGreenNodeInvariants<TGreenNode> where TGreenNode: class, IGreenNode<TGreenNode>
{
    protected abstract TGreenNode create(IReadOnlyList<TGreenNode> elements);
    [InvariantMethod]
    public void TestElementsNotNull(TGreenNode node)
    {
        Contract.InvariantForAll(node.GetDescendants(), element => element is not null);
    }
    [InvariantMethod]
    public void TestElementsNotCircular(TGreenNode node)
    {
        CircularDependencyTracker<TGreenNode> circularDependencyDetector = new();
        foreach (var relation in node.GetDescendantsWithParent())
        {
            circularDependencyDetector.Add(relation.Parent, relation.Child);
        }
    }
    [TestMethod]
    public void TestWithEmptyElementsHasEmptyElements()
    {
        var node = create(EmptyCollection<TGreenNode>.ReadOnlyList);
        
        Contract.AssertSequenceEqual(node.Elements, new TGreenNode[0]);
    }
    [TestMethod]
    public void TestWithSingleElementHasSingleElement()
    {
        var element = this.create(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element });

        Contract.AssertSequenceEqual(node.Elements, new[] { element });
    }
    [TestMethod]
    public void TestWithTwoElementsHasTwoElements()
    {
        var element = this.create(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.create(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element, element2});

        Contract.AssertSequenceEqual(node.Elements, new[] { element, element2 });
    }
}

[TestClass]
public class TrivialGreenNodeTests : IGreenNodeInvariants<TrivialGreenNode>
{
    protected override TrivialGreenNode create(IReadOnlyList<TrivialGreenNode> elements) => new TrivialGreenNode(elements);
}



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
