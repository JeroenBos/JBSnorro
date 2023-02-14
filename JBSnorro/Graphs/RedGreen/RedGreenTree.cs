//using JBSnorro.Diagnostics;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace JBSnorro.Graphs.RedGreen;

//public sealed class RedGreenTree<T> : RedGreenTree<RedNode<T>, T>
//{
//    internal RedGreenTree(GreenNode<T> data)
//        : base(data, (tree, data2) => new RedNode<T>((RedGreenTree<T>)tree, data2))
//    {
//    }

//    protected internal override RedGreenTree<RedNode<T>, GreenNode<T>, T> CreateNewTree(GreenNode<T> data)
//    {
//        return new RedGreenTree<T>(data);
//    }
//}
//public class RedGreenTree<TNode, T> : RedGreenTree<TNode, GreenNode<T>, T>
//    where TNode : RedNode<TNode, T>
//{
//    internal RedGreenTree(GreenNode<T> data, Func<RedGreenTree<TNode, GreenNode<T>, T>, GreenNode<T>, TNode> ctor)
//        : base(data, ctor, GreenNode<T>.Create)
//    {
//    }

//    protected internal override RedGreenTree<TNode, GreenNode<T>, T> CreateNewTree(GreenNode<T> data)
//    {
//        return new RedGreenTree<TNode, T>(data, base.PassableToken.RedConstructor);
//    }
//}
//public class RedGreenTree<TNode, TGreenNode, T>
//    where TNode : RedNode<TNode, TGreenNode, T>
//    where TGreenNode : GreenNode<TGreenNode, T>
//{
//    internal TNode this[Stack<int> route]
//    {
//        get
//        {
//            return this.Root.Walk(route);
//        }
//    }
//    public TNode Root { get; }

//    internal RedGreenTree(TNode root,
//                          Func<T, TGreenNode> ctor1,
//                          Func<RedGreenTree<TNode, TGreenNode, T>, TGreenNode, TNode> ctor2)
//    {
//        Contract.Requires(root != null);
//        Contract.Requires(root.Tree == null);
//        Contract.Requires(root.Parent == null);

//        this.Root = root;
//        this.greenConstructor = ctor1;
//        this.redConstructor = ctor2;
//    }
//    internal RedGreenTree(TGreenNode data,
//                          Func<RedGreenTree<TNode, TGreenNode, T>, TGreenNode, TNode> ctor2,
//                          Func<T, TGreenNode> ctor1)
//    {
//        Contract.Requires(data != null);
//        Contract.Requires(ctor2 != null);

//        this.Root = ctor2(this, data);
//        this.greenConstructor = ctor1;
//        this.redConstructor = ctor2;
//    }

//    /// <summary>
//    /// Creates a new tree where the contents of the specified node are replaced by the specified contents.
//    /// </summary>
//    internal RedGreenTree<TNode, TGreenNode, T> Substitute(TNode node, TGreenNode newNode)
//    {
//        Contract.Requires(node != null);
//        Contract.Requires(newNode != null);
//        Contract.Requires(ReferenceEquals(node.Tree, this));

//        return node.With(newNode, null);
//    }
//    internal RedGreenTree(TGreenNode data, ConstructorPassableToken passableToken)
//        : this(data, passableToken.RedConstructor, passableToken.GreenConstructor)
//    {
//    }

//    public RedGreenTree<TNode, TGreenNode, T> WithParent(T value)
//    {
//        var rootData = this.Root.data.WithParent(value);
//        return this.CreateNewTree(rootData);
//    }
//    private readonly Func<T, TGreenNode> greenConstructor;
//    private readonly Func<RedGreenTree<TNode, TGreenNode, T>, TGreenNode, TNode> redConstructor;

//    public TGreenNode Construct(T value)
//    {
//        return greenConstructor(value);
//    }
//    protected internal virtual RedGreenTree<TNode, TGreenNode, T> CreateNewTree(TGreenNode data)
//    {
//        return new RedGreenTree<TNode, TGreenNode, T>(data, this.PassableToken);
//    }

//    public ConstructorPassableToken PassableToken => new ConstructorPassableToken(this);
//    public struct ConstructorPassableToken
//    {
//        internal Func<T, TGreenNode> GreenConstructor { get; }
//        internal Func<RedGreenTree<TNode, TGreenNode, T>, TGreenNode, TNode> RedConstructor { get; }

//        public ConstructorPassableToken(RedGreenTree<TNode, TGreenNode, T> tree)
//        {
//            Contract.Requires(tree != null);

//            this.GreenConstructor = tree.greenConstructor;
//            this.RedConstructor = tree.redConstructor;
//        }
//    }
//}
