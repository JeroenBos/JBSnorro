//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace JBSnorro.Graphs
//{
//	public class StructureProvidedNode<TSource, TDerived, TValue> : StructureProvidedNode<TSource, TDerived> where TDerived : StructureProvidedNode<TSource, TDerived, TValue> where TSource : INode<TSource>
//	{
//		public TValue Value { get; }

//		public StructureProvidedNode(TSource node, TValue value, Func<TSource, TDerived> map) : base(node, map)
//		{
//			this.Value = value;
//		}
//	}
//}
