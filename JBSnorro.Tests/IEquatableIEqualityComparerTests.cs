using JBSnorro;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestEqualityComparerThroughIEquatable
{
    [TestMethod]
    public void WithReferenceType()
    {
        var iequalityComparer = InterfaceWraps.GetIEquatableEqualityComparer(typeof(RefType));

        var a = new RefType();
        Assert.IsFalse(iequalityComparer.Equals(a, new RefType()));
        Assert.IsTrue(iequalityComparer.Equals(a, a));
    }
    class RefType : IEquatable<RefType>
    {
        static int counter = 0;
        private readonly int id = ++counter;
        public bool Equals(RefType other)
        {
            return other?.id == this.id;
        }
    }

    [TestMethod]
    public void WithValueType()
    {
        var iequalityComparer = InterfaceWraps.GetIEquatableEqualityComparer(typeof(RefType));

        var a = new RefType();
        Assert.IsFalse(iequalityComparer.Equals(a, new RefType()));
        Assert.IsTrue(iequalityComparer.Equals(a, a));
    }
    struct ValueType : IEquatable<ValueType>
    {
        static int counter = 0;
        private readonly int id = ++counter;
        public bool Equals(ValueType other)
        {
            return other.id == this.id;
        }
        public ValueType() { }
    }
}
