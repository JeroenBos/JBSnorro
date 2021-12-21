using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Graphs
{
	public class StructureProvidedNode<TSource, TDerived> : INode<TDerived> where TDerived : StructureProvidedNode<TSource, TDerived> where TSource : INode<TSource>
	{
		protected readonly TSource SourceNode;
		private readonly Func<TSource, TDerived> map;
		private IReadOnlyList<TDerived> elements;

		public IReadOnlyList<TDerived> Elements
		{
			get
			{
				if (elements == null)
				{
					elements = SourceNode.Elements.MapLazily(map);
				}
				return elements;
			}
		}
		public TDerived Parent => map(SourceNode.Parent);

		public StructureProvidedNode(TSource node, Func<TSource, TDerived> map)
		{
			Contract.Requires(node != null);
			Contract.Requires(map != null);

			this.map = map;
			this.SourceNode = node;
		}

		INode INode.Parent => this.Parent;
		IReadOnlyList<INode> INode.Elements => this.Elements;
		IReadOnlyList<IGreenNode> IGreenNode.Elements => this.Elements;
	}
}
