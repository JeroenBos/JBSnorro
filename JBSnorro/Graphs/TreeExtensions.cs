using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Graphs;


public interface ITreeNode
{
    bool IsLeaf { get; }
    IEnumerable<ITreeNode> GetChildren();
    ITreeNode Parent { get; }
}

/// <summary> This class contains methods that can be implemented on trees, that is, non-cyclic graphs. The generic parameter is the node type. </summary>
public static class TreeExtensions
{
    /// <summary> Gets the children of the tree node, or an empty enumerable if it is a leaf. </summary>
    public static IEnumerable<ITreeNode> GetChildrenOrEmpty(this ITreeNode node)
    {
        Contract.Requires(node != null);

        return node.IsLeaf ? EmptyCollection<ITreeNode>.ReadOnlyList : node.GetChildren();
    }
    public static IEnumerable<int> GetIndicesOfGroundedSubtrees(this ITreeNode root,
                                                                ITreeNode subtreeToFind,
                                                                Func<ITreeNode, ITreeNode, bool> nodesEqual,
                                                                Func<ITreeNode, ITreeNode, ITreeNode, ITreeNode, bool> edgesEqual)
    {
        Contract.Requires(root != null);
        Contract.Requires(subtreeToFind != null);
        Contract.Requires(nodesEqual != null);
        Contract.Requires(edgesEqual != null);

        var leavesToFind = subtreeToFind.GetLeaves(GetChildrenOrEmpty).ToList();
        var leaves = root.GetLeaves(GetChildrenOrEmpty).ToList();
        foreach (int possibleStartIndexOfSubtree in leaves.IndicesOf(leavesToFind, nodesEqual))
        {
            int generationsUpToSubtreeToFindRoot = subtreeToFind.CountGenerationsTo(leavesToFind[0]);
            var possibleRoot = leaves[possibleStartIndexOfSubtree].GetAncestorsAndSelf().ElementAt(generationsUpToSubtreeToFindRoot);

            if (TreeStructuresMatch(subtreeToFind, possibleRoot))
                yield return possibleStartIndexOfSubtree;
        }
    }

    /// <summary> Gets whether the two specified node have equal tree structure, that is, whether they have the same number of children nodes, and so do their children in the same order, recursively. </summary>
    public static bool TreeStructuresMatch(ITreeNode root1, ITreeNode root2)
    {
        Contract.Requires(root1 != null);
        Contract.Requires(root2 != null);

        return root1.GetChildrenOrEmpty().SequenceEqual(root2.GetChildrenOrEmpty(), TreeStructuresMatch);
    }

    /// <summary> Gets how many generations there are between the two specified node. </summary>
    /// <param name="descendent"> A descendent of the node. </param>
    /// <returns> 0 if the two specified nodes are equal, 1 if the descendent is a direct child, 2 for grandchild, etc </returns>
    public static int CountGenerationsTo(this ITreeNode node, ITreeNode descendent)
    {
        Contract.Requires(node != null);
        Contract.Requires(descendent != null);

        int result = 0;
        while (descendent != node)
        {
            Contract.Requires(descendent != null, "The 'descendent' does not descend from the specified node");
            descendent = descendent.Parent;
            result++;
        }
        return result;
    }

    /// <summary> Gets all ancestors of the specified node. </summary>
    public static IEnumerable<ITreeNode> GetAncestors(this ITreeNode node)
    {
        Contract.Requires(node != null);

        while (node.Parent != null)
        {
            node = node.Parent;
            yield return node;
        }
    }
    /// <summary> Gets all ancestors of the specified node, and the node itself. </summary>
    public static IEnumerable<ITreeNode> GetAncestorsAndSelf(this ITreeNode node)
    {
        Contract.Requires(node != null);

        while (node != null)
        {
            yield return node;
            node = node.Parent;
        }
    }

    /// <summary> This method traverses the tree in a breadth-first manner. </summary>
    /// <param name="root"> The node to start on. </param>
    /// <param name="getChildren"> A function yielding the nodes connected to the specified node. </param>
    public static IEnumerable<TNode> TraverseBreadthFirst<TNode>(TNode root, Func<TNode, IEnumerable<TNode>> getChildren)
    {
        Contract.Requires(getChildren != null);

        var stack = new Stack<TNode>();
        stack.Push(root);
        while (stack.Count != 0)
        {
            TNode item = stack.Pop();
            yield return item;
            foreach (var child in getChildren(item))
            {
                stack.Push(child);
            }
        }
    }
    /// <summary> This method traverses the tree in a breadth-first manner. </summary>
    /// <param name="roots"> The node to start from. They are started from in depth-first manner. </param>
    /// <param name="getChildren"> A function yielding the nodes connected to the specified node. </param>
    public static IEnumerable<TNode> TraverseBreadthFirst<TNode>(IEnumerable<TNode> roots, Func<TNode, IEnumerable<TNode>> getChildren)
    {
        return roots.Select(root => TraverseBreadthFirst(root, getChildren)).Concat();
    }



    /// <summary> Gets the leaves that are in the specified tree. A leaf is one where getChildren returns null. </summary>
    /// <param name="getChildren"> The function getting the children of some specified root. Return an empty collection if it is a leaf. </param>
    public static IEnumerable<T> GetLeaves<T>(this T root, Func<T, IEnumerable<T>> getChildren) where T : class
    {
        Contract.Requires(root != null);
        Contract.Requires(getChildren != null);

#if DEBUG
        HashSet<T> usedNodes = new HashSet<T>(ReferenceEqualityComparer.Instance); // debugging purposes only
#endif

        var result = GetLeaves(root).EnsureSingleEnumerationDEBUG();
        Contract.Ensures(result.All(resultNode => resultNode != null));
        return result;

        IEnumerable<T> GetLeaves(T node)
        {
#if DEBUG
            Contract.Requires(node != null);
            Contract.Requires(usedNodes.Add(node), "Circular reference in tree");
#endif

            var children = getChildren(node).EnsureSingleEnumerationDEBUG();
            Contract.Assert(children is not null);

            bool isLeaf = IsEmpty(ref children);
            if (isLeaf)
            {
                return node.ToSingleton();
            }
            else
            {
                Contract.Assert(children != null);
                Contract.AssertForAll(children, child => child != null);
                return children.SelectMany(child => GetLeaves(child));
            }
        }
    }
    /// <summary> Gets the root of a tree. </summary>
    /// <param name="node"> The node of the tree for which to get the root. </param>
    /// <param name="getParent"> The function that gets the parent of its argument; or null if the argument is the root node. </param>
    public static T GetRoot<T>(this T node, Func<T, T> getParent) where T : class
    {
        Contract.Requires(node != null);
        Contract.Requires(getParent != null);
        Contract.Requires(node.Unfold(t => getParent(t) ?? Option<T>.None).AreUnique(ReferenceEqualityComparer.Instance), "Circular reference");

        var parent = getParent(node);
        while (parent != null)
        {
            node = parent;
            parent = getParent(node);
        }

        Contract.Ensures(node != null);
        return node;
    }
    /// <summary>
    /// Gets all descendants of the specified node, depth-first.
    /// </summary>
    public static IEnumerable<T> GetDescendants<T>(this T node, Func<T, IEnumerable<T>> getChildren)
    {
        foreach (var child in getChildren(node))
        {
            yield return child;
            foreach (var descendant in GetDescendants(child, getChildren))
            {
                yield return descendant;
            }
        }
    }
    /// <summary>
    /// Gets all descendants of the specified node, and the node itself, depth-first.
    /// </summary>
    public static IEnumerable<T> GetDescendantsAndSelf<T>(this T node, Func<T, IEnumerable<T>> getChildren)
    {
        yield return node;
        foreach (var descendant in GetDescendants(node, getChildren))
        {
            yield return descendant;
        }
    }
    /// <summary>
    /// Gets all descendants of the specified node together with their respective parent, depth-first.
    /// </summary>
    public static IEnumerable<ParentChild<T>> GetDescendantsWithParent<T>(this T node, Func<T, IEnumerable<T>> getChildren)
    {
        foreach (var child in getChildren(node))
        {
            yield return new ParentChild<T> { Child = child, Parent = node };
            foreach (var descendant in GetDescendants(child, getChildren))
            {
                yield return new ParentChild<T> { Child = descendant, Parent = child };
            }
        }
    }
    /// <summary>
    /// A combination of a child and parent.
    /// </summary>
    public readonly record struct ParentChild<T>
    {
        public required T Parent { get; init; }
        public required T Child { get; init; }
    }
}
