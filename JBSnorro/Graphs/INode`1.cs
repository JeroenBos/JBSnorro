using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Graphs
{
	/// <summary>
	/// A node on which only children can be queried, not parent.
	/// </summary>
	public interface IGreenNode
	{
		IReadOnlyList<IGreenNode> Elements { get; }
	}
	public interface IGreenNode<out TNode> : IGreenNode
		where TNode : IGreenNode<TNode>
	{
		new IReadOnlyList<TNode> Elements { get; }
	}
	public interface IGreenNode<out TNode, out T> : IGreenNode<TNode>
	  where TNode : IGreenNode<TNode, T>
	{
		T Value { get; }
		new IReadOnlyList<TNode> Elements { get; }
	}
	public interface INode : IGreenNode
	{
		INode Parent { get; }
		new IReadOnlyList<INode> Elements { get; }
	}
	public interface INode<out TNode> : INode, IGreenNode<TNode> where TNode : INode<TNode>
	{
		new TNode Parent { get; }
		new IReadOnlyList<TNode> Elements { get; }
	}
}
