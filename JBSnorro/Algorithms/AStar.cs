#nullable disable
using JBSnorro.Collections;
using System.Diagnostics.Contracts;

/// <summary> Encapsulates finding a route using the A* algorithm. </summary>
public static class AStar
{
	/// <summary> Returns all values of the nodes on the shortest path between any initial and a goal node. Returns null if there is no path. </summary>
	/// <param name="initialSearchSet"> All initial nodes. The A* algorithm will start searching from these nodes. </param>
	/// <param name="getLinkedNodes"> For any node, this function returns all linked nodes, that is all nodes adjacent to the parameter node. </param>
	/// <param name="heuristic"> The heuristic function, representing the approximated cost of moving to a final node. 
	/// Takes the arguments TValue node and int G at the location from which the cost is to be approximated. </param>
	/// <param name="calculateG"> A function which returns the total cost (the G) of node advanced to. The first two parameters are the nodes advanced from and to. 
	/// The thrid parameter is the G of the node moved from. </param>
	/// <param name="isGoal"> This function returns whether the specified node is a final node. </param>
	/// <param name="getInitialG"> Any initial node may start with an initial G, this function is the mapping. </param>
	/// <param name="getLength"> Represents a function returning a scalar representation of the parameter G.</param>
	/// <returns> Returns the cheapest path of nodes, from beginning till end. Returns null if there is no path. </returns>
	/// <typeparam name="TNode"> The value an A* node is identified by. There must be a one-on-one correspondence between the identifiers and the nodes. </typeparam>
	/// <typeparam name="TG"> The type of a G of a node. This G must have a scalar value acquirable through the specified getLength method. 
	/// It represents the type of the cost that needs to be minimized for the optimal solution. </typeparam>
	public static IEnumerable<TNode> FindRoute<TNode, TG>(IEnumerable<TNode> initialSearchSet,
														  Func<TNode, IEnumerable<TNode>> getLinkedNodes,
														  Func<TNode, TG, int> heuristic,
														  Func<TNode, TNode, TG, TG> calculateG,
														  Func<TNode, bool> isGoal,
														  Func<TNode, TG> getInitialG,
														  Func<TG, int> getLength) where TG : IComparable<TG>
	{   // argument checking omitted
		Contract.Requires(initialSearchSet != null);
		Contract.Requires(getLinkedNodes != null);
		Contract.Requires(heuristic != null);
		Contract.Requires(calculateG != null);
		Contract.Requires(isGoal != null);
		Contract.Requires(getInitialG != null);

		Node<TNode, TG>.GetLength = getLength;
		var openlist = new Heap<Node<TNode, TG>>(initialSearchSet.Select(i => new Node<TNode, TG>(null, i, getInitialG(i), heuristic)));
		var closedList = new List<Node<TNode, TG>>();
		while (openlist.Count != 0)
		{
			var currentNode = openlist.RemoveNext();
			if (isGoal(currentNode.Value))
			{
				return currentNode.Route;
			}
			closedList.Add(currentNode);
			foreach (var linkedNode in getLinkedNodes(currentNode.Value)) // linkedNode is a neighbor of the current node
			{
				if (!closedList.Any(node => linkedNode.Equals(node.Value))) // if there are no nodes with the value linkedNode on the closed list
				{
					TG newG = calculateG(currentNode.Value, linkedNode, currentNode.G);
					var openNode = openlist.FirstOrDefault(node => linkedNode.Equals(node.Value));
					if (openNode == null)
					{
						openlist.Add(new Node<TNode, TG>(currentNode, linkedNode, newG, heuristic));
					}
					else if (newG.CompareTo(openNode.G) < 0) // if new route is shorter
					{
						openNode.Parent = currentNode;
						openNode.G = newG;
					}
				}
			}
		}
		return null;
	}
	/// <summary> A simpler overload where TG is taken to be an integer, in which case default arguments to "getInitialG" and "getLength" can be specified. </summary>
	public static IEnumerable<T> FindRoute<T>(IEnumerable<T> initialSearchSet,
											  Func<T, IEnumerable<T>> getLinkedNodes,
											  Func<T, int, int> heuristic,
											  Func<T, T, int, int> calculateG,
											  Func<T, bool> isGoal)
	{
		return FindRoute<T, int>(initialSearchSet, getLinkedNodes, heuristic, calculateG, isGoal, t => 0, _ => _);
	}


	private sealed class Node<T, TG> : IComparable<Node<T, TG>> where TG : IComparable<TG>
	{
		internal static Func<TG, int> GetLength;


		private Func<T, TG, int> heuristic;
		public readonly T Value;
		public Node<T, TG> Parent { get; internal set; }
		private long F
		{
			get { return GetLength(G) + H; }
		}
		public TG G { get; internal set; }
		private long H
		{
			get { return heuristic(this.Value, this.G); }
		}

		internal Node(Node<T, TG> parent, T value, TG g, Func<T, TG, int> heuristic)
		{
			this.Parent = parent;
			this.Value = value;
			this.G = g;
			this.heuristic = heuristic;
		}

		public Stack<T> Route
		{
			get
			{
				Stack<T> route = new Stack<T>();
				Node<T, TG> n = this;
				while (n.Parent != null)
				{
					route.Push(n.Value);
					n = n.Parent;
				}
				route.Push(n.Value);
				return route;
			}
		}

		public override string ToString()
		{
			return string.Format("F = {0}, G = {1}, H = {2}, Value = {3}", F, G, H, Value);
		}
		public int CompareTo(Node<T, TG> other)
		{
			int f = this.F.CompareTo(other.F);
			if (f != 0)
				return f;
			int g = this.G.CompareTo(other.G);
			if (g != 0)
				return g;
			return this.H.CompareTo(other.H);
		}
	}
}
