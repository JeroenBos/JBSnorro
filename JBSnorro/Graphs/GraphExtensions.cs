//using JBSnorro.Diagnostics;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace JBSnorro.Graphs
//{
//	public static class GraphExtensions
//	{
//		/// <summary> Transitively selects all elements in the specified sequence, in a depth-first manner. </summary>
//		public static IEnumerable<T> TransitiveSelect<T>(this T root) where T : INode<T>
//		{
//			return EnumerableExtensions.TransitiveSelect(root, node => node.Elements);
//		}
//		/// <summary> Transitively selects all elements in the specified sequence, in a depth-first manner. </summary>
//		public static IEnumerable<T> TransitiveSelect<T, TValue>(this T root) where T : INode<T, TValue>
//		{
//			//TODO: this overload is redundant because of the other right?
//			return EnumerableExtensions.TransitiveSelect(root, node => node.Elements);
//		}
//		/// <summary>
//		/// Gets the root of the tree the specified node is part of.
//		/// </summary>
//		public static T GetRoot<T>(this T node) where T : class, INode<T>
//		{
//			return EnumerableExtensions.GetRoot(node, n => n.Parent);
//		}
//		/// <summary>
//		/// Gets whether the specified node is the root of its tree.
//		/// </summary>
//		public static bool IsRoot<T>(this T node) where T : class, INode<T>
//		{
//			return node.Parent == null;
//		}
//		/// <summary>
//		/// Gets whether the specified node is the root of its tree.
//		/// </summary>
//		public static bool IsRoot<T, TValue>(this T node) where T : class, INode<T, TValue>
//		{
//			return node.Parent == null;
//		}
//		/// <summary>
//		/// Gets whether the specified node is a leaf node.
//		/// </summary>
//		public static bool IsLeaf<T>(this T node) where T : class, INode
//		{
//			return node.Elements.Count == 0;
//		}
//		/// <summary>
//		/// Gets whether the specified node is a leaf node.
//		/// </summary>
//		public static bool IsAtomic<T>(this T node) where T : class, INode
//		{
//			return IsLeaf(node);
//		}
//		/// <summary>
//		/// Gets whether the specified node is a leaf node.
//		/// </summary>
//		public static bool IsAtomic<T, TValue>(this T node) where T : class, INode<T, TValue>
//		{
//			return IsLeaf(node);
//		}

//		/// <summary>
//		/// Gets whether the specified node is a leaf node.
//		/// </summary>
//		public static bool IsLeaf<T, TValue>(this T node) where T : class, INode<T, TValue>
//		{
//			return node.Elements.Count == 0;
//		}
//		/// <summary>
//		/// Gets whether the specified node is not a leaf node.
//		/// </summary>
//		public static bool IsComposite<T>(this T node) where T : class, INode
//		{
//			return !IsLeaf(node);
//		}
//		/// <summary>
//		/// Gets whether the specified node is not a leaf node.
//		/// </summary>
//		public static bool IsComposite<T, TValue>(this T node) where T : class, INode<T, TValue>
//		{
//			return !IsLeaf(node);
//		}
//		/// <summary>
//		/// Gets the leaves under the specified node.
//		/// </summary>
//		public static IEnumerable<T> GetLeaves<T>(this T node) where T : class, INode<T>
//		{
//			return EnumerableExtensions.GetLeaves(node, n => n.Elements);
//		}
//		/// <summary>
//		/// Gets the leaves under the specified node.
//		/// </summary>
//		public static IEnumerable<T> GetLeaves<T, TValue>(this T node) where T : class, INode<T, TValue>
//		{
//			return EnumerableExtensions.GetLeaves(node, n => n.Elements);
//		}
//		/// <summary>
//		/// Returns a cross-section of nodes that match the specified predicate, or leaf nodes, of the tree under the specified node.
//		/// </summary>
//		/// <param name="predicate"> The function selecting the cross-sectional nodes. </param>
//		public static IEnumerable<T> Slice<T>(this T root, Func<T, bool> predicate) where T : IGreenNode
//		{
//			if (root.Elements.Count == 0 || predicate(root))
//			{
//				return new[] { root };
//			}
//			else
//			{
//				return root.SliceWithoutRoot(predicate);
//			}
//		}
//		/// <summary>
//		/// Returns a cross-section of nodes that match the specified predicate, or leaf nodes, of the tree under the specified node, excluding the root.
//		/// This is identical to <see cref="Slice{T}(T, Func{T, bool})"/> except in the case that the root is a leaf node: this does not yield the root
//		/// whereas <see cref="Slice{T}(T, Func{T, bool})"/> does. 
//		/// </summary>
//		/// <param name="predicate"> The function selecting the cross-sectional nodes. </param>
//		public static IEnumerable<T> SliceWithoutRoot<T>(this T root, Func<T, bool> predicate) where T : IGreenNode
//		{
//			foreach (var result in root.Elements.Cast<T>().SelectMany(node => node.Slice(predicate)))
//				yield return result;
//		}
//		/// <summary>
//		/// Returns a cross-section of nodes that match the specified predicate of the tree under the specified node.
//		/// The cross-section has holes for each path from root node to leaves where no node matches the predicate.
//		/// </summary>
//		/// <param name="predicate"> The function selecting the cross-sectional nodes. </param>
//		public static IEnumerable<T> SliceWithoutLeaves<T>(this T root, Func<T, bool> predicate) where T : IGreenNode
//		{
//			Contract.Requires(root != null);
//			Contract.Requires(predicate != null);

//			if (root.Elements.Count == 0 || predicate(root))
//			{
//				yield return root;
//			}
//			else
//			{
//				foreach (var result in root.Elements.Cast<T>().SelectMany(node => node.Slice(predicate)))
//					yield return result;
//			}
//		}
//		/// <summary>
//		/// Creates a new tree from the specified tree with only the nodes that match the specified predicate. 
//		/// </summary>
//		/// <param name="root"> The node representing the top of the tree to create a squashed version of. </param>
//		/// <param name="predicate"> The function determining whether a node should be copied to the new tree. </param>
//		/// <param name="ctor"> A function creating a node in the resulting tree. </param>
//		/// <returns></returns>
//		public static TResult Squash<T, TResult>(this T root, Func<T, bool> predicate, Func<T, IReadOnlyList<TResult>, TResult> ctor)
//			where T : IGreenNode<T>
//			where TResult : class
//		{
//			Contract.Requires(predicate != null);
//			Contract.Requires(predicate(root), "The root element must satisfy the predicate, otherwise we may end up with multiple trees. ");
//			Contract.Requires(ctor != null);

//			return squash(root);

//			TResult squash(T node)
//			{
//				Contract.Requires(node != null);
//				if (node.Elements.Count == 0)
//				{
//					if (predicate(node))
//						return ctor(node, EmptyCollection<TResult>.ReadOnlyList);
//					else
//						throw new Exception("A leaf element does not match the predicate. "); // this could be the root too
//				}
//				else
//				{
//					var elements = node.Elements
//									   .SelectMany(element => element.Slice(predicate).Select(squash))
//									   .ToReadOnlyList();


//					return ctor(node, elements);
//				}
//			}
//		}

//		/// <summary> Gets the ancestors of the specified name. May be empty. </summary>
//		public static IEnumerable<T> GetAncestors<T>(this T node) where T : class, INode<T>
//		{
//			Contract.Requires(node != null);

//			var parent = node.Parent;
//			while (parent != null)
//			{
//				yield return parent;
//				parent = parent.Parent;
//			}
//		}
//		/// <summary> Gets the ancestors of the specified name, starting with the name itself. </summary>
//		public static IEnumerable<T> GetAncestorsAndSelf<T>(this T node) where T : class, INode<T>
//		{
//			Contract.Requires(node != null);

//			return node.GetAncestors().Prepend(node);
//		}
//		/// <summary> Gets all names downwards in its tree. </summary>
//		public static IEnumerable<T> GetDescendants<T>(this T node) where T : class, INode<T>
//		{
//			Contract.Requires(node != null);

//			return node.GetDescendantsAndSelf().Skip(1);
//		}
//		/// <summary> Gets all names downwards in its tree. </summary>
//		public static IEnumerable<T> GetDescendantsAndSelf<T>(this T node) where T : class, INode<T>
//		{
//			Contract.Requires(node != null);

//			return node.TransitiveSelect(n => n.Elements);
//		}


//		/// <summary> Gets whether the specified tree nodes have identical tree structure with node values equal according to the specified equality comparer. </summary>
//		public static bool SequenceEqual<TNode, UNode, TValue, UValue>(this TNode root1, UNode root2, Func<TValue, UValue, bool> equalityComparer)
//			where TNode : INode<TNode, TValue>
//			where UNode : INode<UNode, UValue>
//		{
//			Contract.Requires(root1 != null);
//			Contract.Requires(root2 != null);
//			Contract.Requires(equalityComparer != null);

//			return SequenceEqual<TNode, UNode>(root1, root2, (tnode, unode) => equalityComparer(tnode.Value, unode.Value));
//		}
//		/// <summary> Gets whether the specified tree nodes have identical tree structure with nodes equal according to the specified equality comparer. </summary>
//		public static bool SequenceEqual<TNode, UNode>(this TNode root1, UNode root2, Func<TNode, UNode, bool> equalityComparer)
//			where TNode : INode<TNode>
//			where UNode : INode<UNode>
//		{
//			Contract.Requires(root1 != null);
//			Contract.Requires(root2 != null);
//			Contract.Requires(equalityComparer != null);

//			if (!equalityComparer(root1, root2))
//				return false;

//			return EnumerableExtensions.SequenceEqual(root1.Elements, root2.Elements, (t, u) => SequenceEqual<TNode, UNode>(t, u, equalityComparer));
//		}
//		public static bool SequenceEqual(this INode root1, INode root2, Func<INode, INode, bool> equalityComparer)
//		{
//			Contract.Requires(root1 != null);
//			Contract.Requires(root2 != null);
//			Contract.Requires(equalityComparer != null);

//			if (!equalityComparer(root1, root2))
//				return false;

//			return EnumerableExtensions.SequenceEqual(root1.Elements, root2.Elements, (t, u) => SequenceEqual(t, u, equalityComparer));
//		}

//		/// <summary>
//		/// Gets whether the specified tree nodes have the same tree structure up to permutations of sibling nodes.
//		/// </summary>
//		public static bool EqualsUpToSiblingPermutations<TNode, UNode>(this TNode root1, UNode root2, Func<TNode, UNode, bool> equalityComparer)
//			where TNode : IGreenNode<TNode>
//			where UNode : IGreenNode<UNode>
//		{
//			Contract.Requires(root1 != null);
//			Contract.Requires(root2 != null);
//			Contract.Requires(equalityComparer != null);

//			return root1.Elements.ContainsSameElements(root2.Elements, transitively);

//			bool transitively(TNode node1, UNode node2)
//			{
//				return equalityComparer(node1, node2) && EqualsUpToSiblingPermutations(node1, node2, equalityComparer);
//			}
//		}
//	}
//}
