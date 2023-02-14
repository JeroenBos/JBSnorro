//using JBSnorro.Diagnostics;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace JBSnorro.Graphs.RedGreen;

//public interface IRedNode<out TNode, TGreenNode, T> where TNode : IRedNode<TNode, TGreenNode, T> where TGreenNode : GreenNode<TGreenNode, T>
//{
//    TGreenNode Data { get; }
//    TNode Parent { get; }
//    IReadOnlyList<TNode> Elements { get; }

//    TNode With(TGreenNode data);
//    int IndexInParent { get; }
//}
//public static class IRedNodeExtensions
//{
//    public static TRedNode GetRoot<TRedNode, TGreenNode, T>(this TRedNode node) where TRedNode : IRedNode<TRedNode, TGreenNode, T> where TGreenNode : GreenNode<TGreenNode, T>
//    {
//        Contract.Requires(node != null);

//        while (node.Parent != null)
//            node = node.Parent;
//        return node;
//    }
//    public static void With<TRedNode, TGreenNode, T>(this TRedNode node, TGreenNode newData) where TRedNode : IRedNode<TRedNode, TGreenNode, T> where TGreenNode : GreenNode<TGreenNode, T>
//    {
//        Contract.Requires(newData != null);

//        var route = new Stack<int>();
//        var newRootData = node.Data.SubstituteFor<TRedNode>(newData, node, route);
//        var root = GetRoot<TRedNode, TGreenNode, T>(node);

//        foreach (var (redNode, greenNode) in Zip<TRedNode, TGreenNode, T>(root, newRootData, route))
//        {
//            redNode.With(greenNode);
//        }
//    }
//    public static IEnumerable<(TRedNode, TGreenNode)> Zip<TRedNode, TGreenNode, T>(TRedNode root, TGreenNode newRootData, Stack<int> route) where TRedNode : IRedNode<TRedNode, TGreenNode, T> where TGreenNode : GreenNode<TGreenNode, T>
//    {
//        while (route.Count != 0)
//        {
//            yield return (root.Elements[route.Peek()], newRootData.Elements[route.Peek()]);
//            route.Pop();
//        }
//    }
//}
