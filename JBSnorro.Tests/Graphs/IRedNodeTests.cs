#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace JBSnorro.Tests.Graphs;

/// <summary>
/// Tests the interface <see cref="IRedNode{TSelf, TGreenNode}"/>.
/// </summary>
[TestCategory("RedGreen")]
public abstract class IRedNodeInvariants<TRedNode, TGreenNode> where TRedNode: class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    protected abstract TGreenNode createGreen(IReadOnlyList<TGreenNode> elements);
    protected abstract TRedNode create(IReadOnlyList<TGreenNode> elements);
    [InvariantMethod] // intention is that this should hold for all instances of TGreenNode. Not sure how to call this though.
    public void TestElementsNotNull(TRedNode node)
    {
        Contract.InvariantForAll(node.GetDescendants<TRedNode, TGreenNode>(), element => element is not null);
    }
    [InvariantMethod]
    public void TestElementsNotCircular(TRedNode node)
    {
        CircularDependencyTracker<TRedNode> circularDependencyDetector = new();
        foreach (var descendant in node.GetDescendants<TRedNode, TGreenNode>())
        {
            if (descendant.Parent is not null)
            {
                circularDependencyDetector.Add(descendant.Parent, descendant);
            }
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
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element });

        Contract.AssertSequenceEqual(node.Elements, new[] { element });
    }
    [TestMethod]
    public void TestWithTwoElementsHasTwoElements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element, element2});

        Contract.AssertSequenceEqual(node.Elements, new[] { element, element2 });
    }
}

[TestClass]
public class TrivialRedNodeTests : IRedNodeInvariants<TrivialRedNode, TrivialGreenNode>
{
    protected override TrivialGreenNode createGreen(IReadOnlyList<TrivialGreenNode> elements) => new TrivialGreenNode(elements);
    protected override TrivialRedNode create(IReadOnlyList<TrivialGreenNode> elements) => new TrivialRedNode(elements);
}
