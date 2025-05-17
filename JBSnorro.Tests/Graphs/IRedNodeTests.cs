using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using JBSnorro.Graphs.RedGreen;
using JBSnorro.Tests.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Graphs;

/// <summary>
/// Tests the interface <see cref="IRedNode{TSelf, TGreenNode}"/>.
/// </summary>
[TestCategory("RedGreen")]
public abstract class IRedNodeInvariants<TRedNode, TGreenNode> where TRedNode: class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
{
    /// <summary>
    /// Gets whether the value of the green part of the red node equals the value of the green node, irrespective of their elements.
    /// </summary>
    protected abstract bool EqualsByValue(TRedNode red, TGreenNode green);
    protected abstract TGreenNode createGreen(IReadOnlyList<TGreenNode> elements);
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
    public void Create_from_green_copies_green_value_and_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element });

        // Act
        var red = TRedNode.Create(green);

        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(red.Elements.Select(red => red.Green), new[] { element });
        Contract.AssertSequenceEqual(green.Elements, new[] { element }, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_from_green_without_elements_has_no_elements()
    {
        var green = createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);

        // Act
        var red = TRedNode.Create(green);

        Contract.AssertSequenceEqual(red.Elements.Select(red => red.Green), new TGreenNode[0]);
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, EmptyCollection<TGreenNode>.ReadOnlyList, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_from_green_with_single_element_has_that_single_element()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element });

        // Act
        var red = TRedNode.Create(green);

        Contract.AssertSequenceEqual(red.Elements.Select(red => red.Green), new[] { element });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, new[] { element }, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_two_elements_has_those_two_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element, element2 });

        // Act
        var red = TRedNode.Create(green);

        Contract.AssertSequenceEqual(red.Elements.Select(red => red.Green), new[] { element, element2 });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, new[] { element, element2 }, "Green node should remain unchanged");
    }
}

[TestClass]
public class TrivialRedNodeTests : IRedNodeInvariants<TrivialRedNode, TrivialGreenNode>
{
    protected override bool EqualsByValue(TrivialRedNode red, TrivialGreenNode green)
    {
        return true; // trivial red and green nodes have no state aside from their elements.
    }
    protected override TrivialGreenNode createGreen(IReadOnlyList<TrivialGreenNode> elements) => new TrivialGreenNode(elements);
}
