using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Graphs.RedGreen;

public sealed class RedNode<T> : RedNode<RedNode<T>, T>
{
    protected override RedNode<T> ctor(GreenNodeWrapper data)
    {
        return new RedNode<T>(this, data.Data);
    }
    private RedNode(RedNode<T> parent, GreenNode<T> data)
        : base(parent, data, parent.Tree)
    {

    }
    internal RedNode(RedGreenTree<T> tree, GreenNode<T> data)
        : base(tree, data)
    {
    }
    public new RedGreenTree<T> Tree => (RedGreenTree<T>)base.Tree;
}
public abstract class RedNode<TNode, T> : RedNode<TNode, GreenNode<T>, T>
    where TNode : RedNode<TNode, T>
{
    protected RedNode(TNode parent, GreenNodeWrapper data, RedGreenTree<TNode, T> tree)
        : this(parent, data.Data, tree)
    {
    }
    internal RedNode(TNode parent, GreenNode<T> data, RedGreenTree<TNode, T> tree)
        : base(parent, data, tree)
    {
    }
    internal RedNode(RedGreenTree<TNode, T> tree, GreenNode<T> data)
        : base(tree, data)
    {
    }
    public new RedGreenTree<TNode, T> Tree => (RedGreenTree<TNode, T>)base.Tree;
}
public abstract class RedNode<TNode, TGreenNode, T> : IRedNode<TNode, TGreenNode, T>
    where TNode : RedNode<TNode, TGreenNode, T>
    where TGreenNode : GreenNode<TGreenNode, T>
{
    internal readonly TGreenNode data;
    private readonly CachedReadOnlyCollection<TNode> elements;

    protected abstract TNode ctor(GreenNodeWrapper data);

    /// <summary>
    /// Gets the tree this is a node of.
    /// </summary>
    public RedGreenTree<TNode, TGreenNode, T> Tree { get; }
    /// <summary>
    /// Gets the parent node, or null if the current node is the root.
    /// </summary>
    public TNode Parent { get; }
    /// <summary>
    /// Gets the value this node holds.
    /// </summary>
    public T Value => data.Value;
    /// <summary>
    /// Gets the child nodes of the current node.
    /// </summary>
    public IReadOnlyList<TNode> Elements => elements;


    /// <summary>
    /// Creates a root node representing the specified value.
    /// </summary>
    public static RedNode<T> Create(T value)
    {
        var data = GreenNode<T>.Create(value, EmptyCollection<GreenNode<T>>.ReadOnlyList);
        return new RedGreenTree<T>(data).Root;
    }

    internal RedNode(RedGreenTree<TNode, TGreenNode, T> tree, TGreenNode value)
        : this(null, value, tree)
    {
    }
    internal RedNode(TNode parent, TGreenNode data)
        : this(parent, data, parent.Tree)
    {
        Contract.Requires(parent != null);
    }
    protected RedNode(TNode parent, GreenNodeWrapper data, RedGreenTree<TNode, TGreenNode, T> tree)
        : this(parent, data.Data, tree)
    {

    }
    protected RedNode(TNode parent, TGreenNode data, RedGreenTree<TNode, TGreenNode, T> tree)
    {
        Contract.Requires(data != null);
        Contract.Requires(tree != null);
        Contract.Requires(parent == null || parent.Tree == tree);

        this.Parent = parent;
        this.data = data;
        this.Tree = tree;
        this.elements = new CachedReadOnlyCollection<TNode>(this.data.Elements.Count, childIndex => ctor(new GreenNodeWrapper(this.data.Elements[childIndex])));
    }
    protected RedNode(TNode parent, GreenNodeWrapper data)
        : this(parent, data.Data, parent.Tree)
    {
    }

    /// <summary>
    /// This makes a green node passable without having to make it publicly accessible.
    /// </summary>
    protected internal struct GreenNodeWrapper
    {
        internal TGreenNode Data { get; }
        internal GreenNodeWrapper(TGreenNode data)
        {
            Contract.Requires(data != null);

            this.Data = data;
        }
    }


    /// <summary>
    /// Walks the specified route by recursively accessing the child at the popped index and returns the node it arrives at.
    /// </summary>
    /// <param name="route"> The indices per decendant leading to a node. </param>
    internal TNode Walk(Stack<int> route)
    {
        if (route.Count == 0)
            return (TNode)this;

        int childIndex = route.Pop();
        return this.Elements[childIndex].Walk(route);
    }
    /// <summary>
    /// Creates a new tree with a parent of the current node with the specified value. Ancestors of the current node are ignored.
    /// </summary>
    /// <returns> The node parallel to the current node in the newly created tree. </returns>
    public TNode WithParent(T value)
    {
        return this.Tree.WithParent(value).Root.Elements[0];
    }
    /// <summary>
    /// Creates a new tree with the value of the current node replaced by the specified value. Ancestors of the current node are retained.
    /// </summary>
    public TNode With(T value)
    {
        TGreenNode newData = this.data.With(value);
        return this.With(newData);
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except this node has an extra child node inserted at the specified index with the specified value.
    /// </summary>
    public TNode Insert(int index, T value)
    {
        return Insert(index, Tree.Construct(value));
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except this node has an extra child node inserted at the specified index with the specified value.
    /// </summary>
    public TNode Insert(int index, TNode value)
    {
        return Insert(index, value.data);
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except this node has an extra child node inserted at the specified index with the specified value.
    /// </summary>
    internal TNode Insert(int index, TGreenNode value)
    {
        TGreenNode newData = this.data.Insert(index, value);
        return this.With(newData);
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except this node has a child node replaced at the specified index with the specified value.
    /// </summary>
    public TNode Substitute(int index, TNode value)
    {
        return Substitute(index, value.data);
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except this node has a child node replaced at the specified index with the specified value.
    /// </summary>
    public TNode Substitute(int index, TGreenNode value)
    {
        TGreenNode newData = this.data.Substitute(index, value);
        return this.With(newData);
    }
    /// <summary>
    /// Creates a new tree similar to the current tree, except without the child node at the specified index of the current node.
    /// </summary>
    public TNode WithoutAt(int index)
    {
        TGreenNode newData = this.data.RemoveAt(index);
        return this.With(newData);
    }

    /// <summary>
    /// Creates a new tree with the current node and its descendents replaced by the specified subtree. Ancestors of the current node are retained.
    /// </summary>
    internal TNode With(TGreenNode node)
    {
        Contract.Requires(node != null);

        var route = new Stack<int>();
        var newTree = this.With(node, route);
        return newTree[route];
    }

    /// <summary>
    /// Creates a new tree with the current node and its descendents replaced by the specified subtree. Ancestors of the current node are retained.
    /// </summary>
    /// <param name="route"> Is a non-null value is specified, the route from the root to the current node are pushed onto it. </param>
    internal RedGreenTree<TNode, TGreenNode, T> With(TGreenNode node, Stack<int> route = null)
    {
        Contract.Requires(node != null);

        var newRootData = this.data.SubstituteFor(node, this, route);
        var newTree = this.Tree.CreateNewTree(newRootData);
        return newTree;
    }

    TNode IRedNode<TNode, TGreenNode, T>.With(TGreenNode data)
    {
        return this.With(data);
    }

    /// <summary>
    /// Gets the index of the current node in its parents element collection.
    /// </summary>
    internal int IndexInParent
    {
        get
        {
            Contract.Requires<InvalidOperationException>(this.Parent != null);

            return this.Parent.elements.IndexOfCached(element => element == this);
        }
    }

    TGreenNode IRedNode<TNode, TGreenNode, T>.Data => data;
    int IRedNode<TNode, TGreenNode, T>.IndexInParent => IndexInParent;

    public IEnumerable<TNode> TransitiveSelect()
    {
        return ((TNode)this).TransitiveSelect(node => node.Elements);
    }
}
