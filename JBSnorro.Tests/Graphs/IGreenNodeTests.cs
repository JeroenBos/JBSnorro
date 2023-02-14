#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Graphs;

/// <summary>
/// Tests the interface <see cref="IGreenNode{TGreenNode}"/>.
/// </summary>
[TestCategory("RedGreen")]
public abstract class IGreenNodeInvariants<TGreenNode> where TGreenNode: class, IGreenNode<TGreenNode>
{
    protected abstract TGreenNode create(IReadOnlyList<TGreenNode> elements);
    [InvariantMethod] // intention is that this should hold for all instances of TGreenNode. Not sure how to call this though.
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
