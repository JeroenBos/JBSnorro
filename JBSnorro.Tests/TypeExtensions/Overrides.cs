using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.TypeExtensions;

using TypeExtensions = global::JBSnorro.Extensions.TypeExtensions;

[TestClass]
public class Overrides
{
    [TestMethod]
    public void Type_that_overrides_tostring_overrides()
    {
        Contract.Assert(TypeExtensions.OverridesToString(new Overrides()));
    }
    [TestMethod]
    public void Type_that_doesnt_override_tostring_doesnt_override()
    {
        Contract.Assert(!TypeExtensions.OverridesToString(new DoesntOverride()));
    }
    [TestMethod]
    public void Type_that_primitives_override_getHashCode()
    {
        Contract.Assert(TypeExtensions.OverridesToString(0));
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public override string ToString()
    {
        return base.ToString();
    }
    class DoesntOverride
    {
    }
}
