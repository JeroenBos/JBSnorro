#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Graphs;

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
    public void With_no_elements_has_no_elements()
    {
        var node = create(EmptyCollection<TGreenNode>.ReadOnlyList);
        
        Contract.AssertSequenceEqual(node.Elements, new TGreenNode[0]);
    }
    [TestMethod]
    public void With_single_element_has_that_single_element()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element });

        Contract.AssertSequenceEqual(node.Elements, new[] { element });
    }
    [TestMethod]
    public void With_two_elements_has_those_two_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var node = create(new[] { element, element2});

        Contract.AssertSequenceEqual(node.Elements, new[] { element, element2 });
    }


    [TestMethod]
    public void Create_from_green_copies_green_value_and_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element });

        // Act
        var red = TRedNode.Create(green);

        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(red.Elements, new[] { element });
        Contract.AssertSequenceEqual(green.Elements, new[] { element }, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_no_elements_has_no_elements()
    {
        var green = createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);

        // Act
        var red = TRedNode.Create(green, EmptyCollection<TGreenNode>.ReadOnlyList);

        Contract.AssertSequenceEqual(red.Elements, new TGreenNode[0]);
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, EmptyCollection<TGreenNode>.ReadOnlyList, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_single_element_has_that_single_element()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element });

        // Act
        var red = TRedNode.Create(green, new[] { element });

        Contract.AssertSequenceEqual(red.Elements, new[] { element });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, new[] { element }, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_single_element_has_that_single_regardless_of_green_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);

        // Act
        var red = TRedNode.Create(green, new[] { element });

        Contract.AssertSequenceEqual(red.Elements, new[] { element });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, EmptyCollection<TGreenNode>.ReadOnlyList, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_two_elements_has_those_two_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);

        // Act
        var red = TRedNode.Create(green, new[] { element, element2 });

        Contract.AssertSequenceEqual(red.Elements, new[] { element, element2 });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, EmptyCollection<TGreenNode>.ReadOnlyList, "Green node should remain unchanged");
    }
    [TestMethod]
    public void Create_two_elements_has_those_two_elements_regardless_of_green_elements()
    {
        var element = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var element2 = this.createGreen(EmptyCollection<TGreenNode>.ReadOnlyList);
        var green = this.createGreen(new[] { element2 });

        // Act
        var red = TRedNode.Create(green, new[] { element, element2 });

        Contract.AssertSequenceEqual(red.Elements, new[] { element, element2 });
        Contract.Assert(EqualsByValue(red, green));
        Contract.AssertSequenceEqual(green.Elements, new[] { element2 }, "Green node should remain unchanged");
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
    protected override TrivialRedNode create(IReadOnlyList<TrivialGreenNode> elements) => new TrivialRedNode(elements);
}
