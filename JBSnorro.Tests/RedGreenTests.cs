using JBSnorro.Graphs.RedGreen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class RedGreenTests
	{
		[TestMethod]
		public void CreationTest()
		{
			RedNode<int> node = RedNode<int>.Create(0);

			Assert.AreEqual(0, node.Value);
		}
		[TestMethod]
		public void RootDoesntHaveParentTest()
		{
			RedNode<int> node = RedNode<int>.Create(0);

			Assert.IsNull(node.Parent);
		}
		[TestMethod]
		public void Parenting()
		{
			RedNode<int> child = RedNode<int>.Create(0).WithParent(1);
			var parent = child.Parent;

			Assert.AreEqual(1, parent.Elements.Count);
			Assert.AreEqual(1, parent.Value);
			Assert.AreEqual(0, parent.Elements[0].Value);
		}
		[TestMethod]
		public void IdentityEquality()
		{
			var parent = RedNode<int>.Create(0).Insert(0, 1);
			var child1 = parent.Elements[0];
			var child2 = parent.Elements[0];

			Assert.IsTrue(ReferenceEquals(child1, child2));
		}
		[TestMethod]
		public void TransitiveIdentityEquality()
		{
			var parent = RedNode<int>.Create(0).Insert(0, 1);
			var child1 = parent.Elements[0];
			var child2 = child1.Parent.Elements[0];

			Assert.IsTrue(ReferenceEquals(child1, child2));
		}
		[TestMethod]
		public void Insertion()
		{
			var parent = RedNode<int>.Create(0).Insert(0, 1);
			var child = parent.Elements[0];

			Assert.AreEqual(0, parent.Value);
			Assert.AreEqual(1, child.Value);
		}
		[TestMethod]
		public void Removal()
		{
			var node = RedNode<int>.Create(0)
								   .Insert(0, 1)
								   .WithoutAt(0);

			Assert.AreEqual(0, node.Elements.Count);
		}

		[TestMethod]
		public void With()
		{
			RedNode<int> a = RedNode<int>.Create(0)
										 .With(1);

			Assert.AreEqual(1, a.Value);
		}
	}
}
