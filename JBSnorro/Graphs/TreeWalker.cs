using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JBSnorro.Graphs
{
	// There's a strategist with a payload on an initial node.
	// The strategist may choose a navigator, give him the payload, and send him to the initial node.
	// The nagivator may choose a new node, or choose to give the payload back.
	// If a new node is chosen, the strategist chooses a new navigator to send there with the payload, and he is offered the same choice. 
	// If the navigator chooses to give the payload back, then the algorithm terminates if that is the strategist, 
	// otherwise the receiving navigator can choose to visit a new node again, for which the strategist can appoint a navigator, etc.
	// The payload is only allowed to visit each node once.

	public interface IStrategist<TNode, in TPayload>
	{
		INavigator<TNode, TPayload> ChooseNavigator(TNode node);
	}
	public interface INavigator<TNode, in TPayload>
	{
		void Visit(TNode node, IRoute<TNode> route, TPayload payload);
	}

	public interface IVisitor<in TNode, in TPayload>
	{
		void Visit(TNode node, TPayload payload);
	}
	/// <summary>
	/// Proof that a visitor is just a navigator that doesn't go anywhere.
	/// </summary>
	// a navigator is not a visitor!
	public abstract class Visitor<TNode, TPayload> : INavigator<TNode, TPayload>, IVisitor<TNode, TPayload>
	{
		protected abstract void Visit(TNode node, TPayload payload);
		void IVisitor<TNode, TPayload>.Visit(TNode node, TPayload payload) => Visit(node, payload);
		void INavigator<TNode, TPayload>.Visit(TNode node, IRoute<TNode> route, TPayload payload) => Visit(node, payload);
	}

	public abstract class NavigatorRedirecter<TNode, TPayload> : INavigator<TNode, TPayload>
	{
		protected abstract INavigator<TNode, TPayload> GetNavigator(TNode node);

		protected virtual void Visit(TNode node, IRoute<TNode> route, TPayload payload)
		{
			// TODO: cache navigator
			GetNavigator(node).Visit(node, route, payload);
		}

		[DebuggerHidden]
		void INavigator<TNode, TPayload>.Visit(TNode node, IRoute<TNode> route, TPayload payload) => Visit(node, route, payload);
	}

	public class NavigatorDelegateRedirecter<TNode, TPayload> : NavigatorRedirecter<TNode, TPayload>
	{
		private readonly Func<TNode, INavigator<TNode, TPayload>> getNavigator;

		public NavigatorDelegateRedirecter(IStrategist<TNode, TPayload> strategist) : this(strategist.ChooseNavigator) { }
		public NavigatorDelegateRedirecter(Func<TNode, INavigator<TNode, TPayload>> getNavigator)
		{
			Contract.Requires(getNavigator != null);
			this.getNavigator = getNavigator;
		}

		protected override INavigator<TNode, TPayload> GetNavigator(TNode node)
		{
			return getNavigator(node);
		}
	}

	public sealed class SingleStrategist<TNode, TPayload> : IStrategist<TNode, TPayload>
	{
		private readonly INavigator<TNode, TPayload> navigator;
		[DebuggerHidden]
		public SingleStrategist(INavigator<TNode, TPayload> navigator)
		{
			Contract.Requires(navigator != null);
			this.navigator = navigator;
		}

		[DebuggerHidden]
		INavigator<TNode, TPayload> IStrategist<TNode, TPayload>.ChooseNavigator(TNode node) => navigator;
	}

	public sealed class CachedWrappedStrategist<TNode, TPayload> : IStrategist<TNode, TPayload>
	{
		private readonly IStrategist<TNode, TPayload> baseStrategist;
		private readonly Func<INavigator<TNode, TPayload>, INavigator<TNode, TPayload>> getNavigator;
		private readonly Dictionary<INavigator<TNode, TPayload>, INavigator<TNode, TPayload>> cachedWrappedNavigators;

		[DebuggerHidden]
		public CachedWrappedStrategist(
			IStrategist<TNode, TPayload> baseStrategist,
			Func<INavigator<TNode, TPayload>, INavigator<TNode, TPayload>> getNavigator)
		{
			Contract.Requires(baseStrategist != null);
			Contract.Requires(getNavigator != null);

			this.baseStrategist = baseStrategist;
			this.getNavigator = getNavigator;
			this.cachedWrappedNavigators = new Dictionary<INavigator<TNode, TPayload>, INavigator<TNode, TPayload>>(ReferenceEqualityComparer.Instance);
		}

		[DebuggerHidden]
		INavigator<TNode, TPayload> IStrategist<TNode, TPayload>.ChooseNavigator(TNode node)
		{
			var baseNavigator = this.baseStrategist.ChooseNavigator(node);
			return this.cachedWrappedNavigators.GetOrAdd(baseNavigator, this.getNavigator);
		}
	}

	internal abstract class RouteBase<TNode> : IRoute<TNode>
	{
		protected readonly HashSet<TNode> visitedNodes;
		/// <summary>
		/// Gets the equality comparer used for hashing nodes (to check whether it has been visited).
		/// </summary>
		public IEqualityComparer<TNode> EqualityComparer => visitedNodes.Comparer;
		/// <summary>
		/// Gets whether the specified node has been visited.
		/// </summary>
		[DebuggerHidden]
		public bool HasVisited(TNode node) => visitedNodes.Contains(node);
		[DebuggerHidden]
		public void MarkVisited(TNode node)
		{
			Contract.Requires(!this.HasVisited(node), "The specified node has already been marked 'visited'");

			this.TryMarkVisited(node);
		}
		[DebuggerHidden]
		public void TryMarkVisited(TNode node)
		{
			this.visitedNodes.Add(node);
		}
		[DebuggerHidden]
		public RouteBase(IEqualityComparer<TNode>? equalityComparer = null)
		{
			equalityComparer = equalityComparer ?? EqualityComparer<TNode>.Default;
			this.visitedNodes = new HashSet<TNode>(equalityComparer);
		}

		/// <summary>
		/// Visits the specified node.
		/// </summary>
		public abstract void Visit(TNode node);


#if DEBUG
		internal object DebuggingInfo => visitedNodes;
#endif
	}

	// A route for which the navigator is self-visiting, i.e. this route doesn't know anything about a visitor. That's an implementation detail of the navigator
	internal class SelfSufficientRoute<TNode, TPayload> : RouteBase<TNode>
	{
		private readonly TPayload payload;
		private readonly IStrategist<TNode, TPayload> strategist;
		[DebuggerHidden]
		public SelfSufficientRoute(IStrategist<TNode, TPayload> strategist, TPayload payload, IEqualityComparer<TNode>? equalityComparer = null)
			: base(equalityComparer)
		{
			this.payload = payload;
			this.strategist = strategist;
		}


		/// <summary>
		/// Visits the specified node.
		/// </summary>
		public override void Visit(TNode node)
		{
			Contract.Requires(!this.HasVisited(node));

			var navigator = this.strategist.ChooseNavigator(node);
			if (navigator == null)
				throw new ContractException($"{nameof(IStrategist<TNode, TPayload>.ChooseNavigator)} returned null");

			MarkVisited(node);
			navigator.Visit(node, this, this.payload);
		}
	}




	/// <summary>
	/// The user of this library is not expecteded to implement this interface, merely use it.
	/// </summary>
	public interface IRoute<in T>
	{
		/// <summary>
		/// Visits the specified node.
		/// </summary>
		void Visit(T node);
		/// <summary>
		/// Gets whether the specified node has been visited by the current walk.
		/// </summary>
		bool HasVisited(T node);
		/// <summary>
		/// Marks the specified node as visited.
		/// </summary>
		void MarkVisited(T node);
	}

	public static class TreeWalkerExtensions
	{
		public static void Walk<TNode, TPayload>(TNode root,
												 IStrategist<TNode, TPayload> strategySelector,
												 TPayload payload,
												 IEqualityComparer<TNode>? equalityComparer = null)
		{
			var walk = new SelfSufficientRoute<TNode, TPayload>(strategySelector, payload, equalityComparer);
			walk.Visit(root);

#if DEBUG
			DebuggingInfo = walk.DebuggingInfo;
#endif
		}

#if DEBUG
		public static object? DebuggingInfo { get; private set; }
#endif

		/// <param name="navigator"> Always uses this navigator. No strategy selector. </param>
		public static void Walk<TNode, TPayload>(TNode root,
												 INavigator<TNode, TPayload> navigator,
												 TPayload payload,
												 IEqualityComparer<TNode>? equalityComparer = null)
		{
			Walk(root, new SingleStrategist<TNode, TPayload>(navigator), payload, equalityComparer);
		}

		//public static IVisitor<TNode, TPayload> ToVisitor<TNode, TPayload>(this INavigator<TNode, TPayload> navigator)
		//{
		//	return new Visitor<TNode, TPayload>()
		//}
		
	}

	//class NavigatorAsVisitor<TNode, TPayload> : IVisitor<TNode, TPayload>
	//{

	//}

}
