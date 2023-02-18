﻿#nullable enable
using System.Diagnostics;
using static JBSnorro.Graphs.TreeExtensions;

namespace JBSnorro.Graphs;

public static class RedGreenExtensions
{
    public static TGreenNode Substitute<TGreenNode>(this TGreenNode parent, int index, TGreenNode substituter) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count);
        newElements[index] = substituter;
        return parent.With(newElements);
    }
    public static TRedNode Substitute<TRedNode, TGreenNode>(this TRedNode parent, int index, TGreenNode substituter) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count);
        newElements[index] = substituter;
        return parent.With(newElements);
    }
    public static TGreenNode Substitute<TGreenNode>(this TGreenNode parent, Range range, IEnumerable<TGreenNode> substituters) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        int insertIndex = range.GetOffsetAndLength(parent.Elements.Count).Offset;
        var newElements = parent.Elements.ExceptAt(range)
                                         .InsertAt(insertIndex, substituters)
                                         .ToList();
        return parent.With(newElements);
    }
    public static TRedNode Substitute<TRedNode, TGreenNode>(this TRedNode parent, Range range, IEnumerable<TGreenNode> substituters) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        int insertIndex = range.GetOffsetAndLength(parent.Elements.Count).Offset;
        var newElements = parent.Elements.ExceptAt(range)
                                         .InsertAt(insertIndex, substituters)
                                         .ToList();
        return parent.With(newElements);
    }
    public static TGreenNode Insert<TGreenNode>(this TGreenNode parent, int index, TGreenNode item) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + 1);
        newElements.Insert(index, item);
        return parent.With(newElements);
    }
    public static TRedNode Insert<TRedNode, TGreenNode>(this TRedNode parent, int index, TGreenNode item) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + 1);
        newElements.Insert(index, item);
        return parent.With(newElements);
    }
    public static TGreenNode RemoveAt<TGreenNode>(this TGreenNode parent, int index) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(index).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    public static TRedNode RemoveAt<TRedNode, TGreenNode>(this TRedNode parent, int index) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(index).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    public static TGreenNode Insert<TGreenNode>(this TGreenNode parent, int index, IEnumerable<TGreenNode> items) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        if (!items.TryGetNonEnumeratedCount(out int itemsCount))
            itemsCount = 2;

        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + itemsCount);
        newElements.InsertRange(index, items);
        return parent.With(newElements);
    }
    public static TRedNode Insert<TRedNode, TGreenNode>(this TRedNode parent, int index, IEnumerable<TGreenNode> items) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        if (!items.TryGetNonEnumeratedCount(out int itemsCount))
            itemsCount = 2;

        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + itemsCount);
        newElements.InsertRange(index, items);
        return parent.With(newElements);
    }
    public static TGreenNode Insert<TGreenNode>(this TGreenNode parent, int index, IReadOnlyCollection<TGreenNode> items) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + items.Count);
        newElements.InsertRange(index, items);
        return parent.With(newElements);
    }
    public static TRedNode Insert<TRedNode, TGreenNode>(this TRedNode parent, int index, IReadOnlyCollection<TGreenNode> items) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ToList(parent.Elements.Count + items.Count);
        newElements.InsertRange(index, items);
        return parent.With(newElements);
    }
    public static TGreenNode RemoveAt<TGreenNode>(this TGreenNode parent, params int[] indices) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(indices).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    public static TRedNode RemoveAt<TRedNode, TGreenNode>(this TRedNode parent, params int[] indices) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(indices).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    public static TGreenNode RemoveAt<TGreenNode>(this TGreenNode parent, Range range) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(range).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    public static TRedNode RemoveAt<TRedNode, TGreenNode>(this TRedNode parent, Range range) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        List<TGreenNode> newElements = parent.Elements.ExceptAt(range).ToList(parent.Elements.Count - 1);
        return parent.With(newElements);
    }
    /// <summary>
    /// Gets all descendants of the specified node, depth-first.
    /// </summary>
    public static IEnumerable<TGreenNode> GetDescendants<TGreenNode>(this TGreenNode node) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        return TreeExtensions.GetDescendants(node, [DebuggerHidden] (c) => c.Elements);
    }
    /// <summary>
    /// Gets all descendants of the specified node, and the node itself, depth-first.
    /// </summary>
    public static IEnumerable<TGreenNode> GetDescendantsAndSelf<TGreenNode>(this TGreenNode node) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        return TreeExtensions.GetDescendantsAndSelf(node, [DebuggerHidden] (c) => c.Elements);
    }
    /// <summary>
    /// Gets all descendants of the specified node together with their respective parent, depth-first.
    /// </summary>
    public static IEnumerable<ParentChild<TGreenNode>> GetDescendantsWithParent<TGreenNode>(this TGreenNode node) where TGreenNode : class, IGreenNode<TGreenNode>
    {
        return TreeExtensions.GetDescendantsWithParent(node, [DebuggerHidden] (c) => c.Elements);
    }

    /// <summary>
    /// Gets all green descendants of the specified red node, depth-first.
    /// </summary>
    public static IEnumerable<TGreenNode> GetGreenDescendants<TRedNode, TGreenNode>(this TRedNode node) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        foreach (var element in node.Elements)
        {
            yield return element;
            foreach (var descendant in TreeExtensions.GetDescendants<TGreenNode>(element, [DebuggerHidden] (c) => c.Elements))
            {
                yield return descendant;
            }
        }
    }
    /// <summary>
    /// Gets all descendants of the specified red node, depth-first.
    /// </summary>
    public static IEnumerable<TRedNode> GetDescendants<TRedNode, TGreenNode>(this TRedNode node) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        return TreeExtensions.GetDescendants(node, [DebuggerHidden] (c) => c.Elements.Select(TRedNode.Create));
    }
    /// <summary>
    /// Gets all descendants of the specified red node, and the node itself, depth-first.
    /// </summary>
    public static IEnumerable<TRedNode> GetDescendantsAndSelf<TRedNode, TGreenNode>(this TRedNode node) where TRedNode : class, IRedNode<TRedNode, TGreenNode> where TGreenNode : class, IGreenNode<TGreenNode>
    {
        return TreeExtensions.GetDescendantsAndSelf(node, [DebuggerHidden] (c) => c.Elements.Select(TRedNode.Create));
    }

    /// <summary>
    /// Maps one tree structure to another.
    /// </summary>
    public static TResult Map<TGreenNode, TResult>(this TGreenNode node, Func<TGreenNode, TResult> selectorWithoutElements)
        where TGreenNode : class, IGreenNode<TGreenNode>
        where TResult : class, IGreenNode<TResult>
    {
        return node.Map<TGreenNode, TResult>((node, elements) => selectorWithoutElements(node).With(elements));
    }
    /// <summary>
    /// Maps one tree structure to another.
    /// </summary>
    public static TResult Map<TGreenNode, TResult>(this TGreenNode node, Func<TGreenNode, IReadOnlyList<TResult> /*elements*/, TResult> selector)
        where TGreenNode : class, IGreenNode<TGreenNode>
        // where TResultGreenNode : class//, IGreenNode<TResultGreenNode>
    {
        var newElements = node.Elements.Map(element => element.Map(selector));
        return selector(node, newElements);
    }
}