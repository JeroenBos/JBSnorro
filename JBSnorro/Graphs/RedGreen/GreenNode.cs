//using JBSnorro.Diagnostics;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace JBSnorro.Graphs.RedGreen;

///// <summary>
///// A green node with a T Value.
///// </summary>
//public class GenericGreenNode<TNode, T> : IGreenNode<GenericGreenNode<TNode, T>> where TNode : GenericGreenNode<TNode, T>
//{
//    public T Value { get; }
//    public IReadOnlyList<TNode> Elements { get; }

//    public GenericGreenNode(T value, IReadOnlyList<TNode> elements)
//    {
//        Contract.Requires(elements != null);

//        this.Value = value;
//        this.Elements = elements;
//    }


//    protected virtual TNode create(T value, IReadOnlyList<TNode> elements);
//    protected TNode create(T value)
//    {
//        return create(value, EmptyCollection<TNode>.ReadOnlyList);
//    }

//    public TNode Insert(int index, T value)
//    {
//        Contract.Requires(0 <= index && index <= Elements.Count);

//        return Insert(index, create(value));
//    }
//    public TNode Insert(int index, TNode item)
//    {
//        Contract.Requires(0 <= index && index <= Elements.Count);

//        var newElements = Elements.ToList(); //PERF
//        newElements.Insert(index, item);
//        var newReadOnlyElements = newElements.ToReadOnlyList(); //PERF

//        return create(this.Value, newReadOnlyElements);
//    }
//    public TNode RemoveAt(int index)
//    {
//        Contract.Requires(0 <= index && index < Elements.Count);

//        var newElements = Elements.ToList(); //PERF
//        newElements.RemoveAt(index);
//        var newReadOnlyElements = newElements.ToReadOnlyList(); //PERF

//        return create(this.Value, newReadOnlyElements);
//    }
//    /// <summary>
//    /// Substitutes the current node by the specified node. Creates a new tree, including ancestors of the current node, where the current node is replaced by the specified node.
//    /// </summary>
//    /// <param name="substitition"> The node by which the current node is to be replaced. </param>
//    /// <param name="parallel"> The node whose representation is the current green node. This node is used to determine the parents of the current green node. </param>
//    /// <returns> The root node of the tree that resulted from the substitution. </returns>
//    public TNode SubstituteFor<TRedNode>(TNode substitition, TRedNode parallel, Stack<int> route = null) where TRedNode : IRedNode<TRedNode, TNode, T>
//    {
//        Contract.Requires(substitition != null);
//        Contract.Requires(parallel != null);
//        Contract.Requires(ReferenceEquals(parallel.Data, this));

//        if (parallel.Parent == null)
//            return substitition;
//        return Substitute(parallel.IndexInParent, substitition, parallel, route);
//    }

//    /// <summary>
//    /// Substitutes the child node at the specified index for the specified node. 
//    /// </summary>
//    /// <returns> The resulting parent node. </returns>
//    public TNode Substitute(int index, TNode substitution)
//    {
//        Contract.Requires(0 <= index && index < this.Elements.Count);
//        Contract.Requires(substitution != null);

//        var newElements = Elements.ToList(); //PERF
//        newElements[index] = substitution;
//        var newReadOnlyElements = newElements.ToReadOnlyList(); //PERF

//        return create(this.Value, newReadOnlyElements);
//    }
//    /// <summary>
//    /// Substitutes the child node at the specified index for the specified node, and create a new green tree (as opposed to merely the subtree when omitting the RedNode). 
//    /// </summary>
//    /// <param name="parallel"> The node whose representation is the current green node. This node is used to determine the parents of the current green node. </param>
//    /// <returns> The root node of the tree that resulted from the substitution. </returns>
//    public TNode Substitute<TRedNode>(int index, TNode substitution, TRedNode parallel, Stack<int> route) where TRedNode : IRedNode<TRedNode, TNode, T>
//    {
//        Contract.Requires(0 <= index && index < this.Elements.Count);
//        Contract.Requires(substitution != null);
//        Contract.Requires(parallel != null);
//        Contract.Requires(ReferenceEquals(parallel.Data, this));

//        if (route != null)
//            route.Push(index);

//        var newNode = this.Substitute(index, substitution);
//        if (parallel.Parent == null)
//            return newNode;

//        TNode parentData = parallel.Parent.Data;
//        return parentData.Substitute(parallel.IndexInParent, newNode, parallel.Parent, route);
//    }
//    /// <summary>
//    /// Creates a new node with the specified value and the same descendants as the current node.
//    /// </summary>
//    public TNode With(T value)
//    {
//        //TODO: deduplicate
//        return create(value, this.Elements);
//    }
//    public TNode WithParent(T value)
//    {
//        var result = create(value, new ReadOnlyCollection<TNode>(new[] { (TNode)this }));
//        return result;
//    }


//    IReadOnlyList<GenericGreenNode<TNode, T>> IGreenNode<GenericGreenNode<TNode, T>>.Elements => Elements;
//}
