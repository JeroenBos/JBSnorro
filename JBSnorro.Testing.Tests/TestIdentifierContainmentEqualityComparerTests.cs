namespace JBSnorro.Testing.IntertestDependency.Tests;

public class TestIdentifierContainmentEqualityComparerTests
{
    private readonly IEqualityComparer<JBSnorro.Testing.IntertestDependency.TestIdentifier> comparer = TestIdentifier.TestIdentifierContainmentEqualityComparerInstance;

    private readonly TestIdentifier TypeT = new TestIdentifier("T", isType: true);
    private readonly TestIdentifier TypeT2 = new TestIdentifier("T", isType: true);

    private readonly TestIdentifier T_M = new TestIdentifier("T.M", isType: false);
    private readonly TestIdentifier T_M2 = new TestIdentifier("T.M", isType: false);

    private readonly TestIdentifier T_N = new TestIdentifier("T.N", isType: false);
    private readonly TestIdentifier T_N2 = new TestIdentifier("T.N", isType: false);

    private readonly TestIdentifier TypeU = new TestIdentifier("U", isType: true);
    private readonly TestIdentifier TypeU2 = new TestIdentifier("U", isType: true);

    private readonly TestIdentifier U_M = new TestIdentifier("U.M", isType: false);
    private readonly TestIdentifier U_M2 = new TestIdentifier("U.M", isType: false);


    [@Fact]
    public void Equal_Types_Are_Equal()
    {
        Assert.True(comparer.Equals(TypeT, TypeT2));
    }

    [@Fact]
    public void Unequal_Types_Are_unequal()
    {
        Assert.False(comparer.Equals(TypeT, TypeU));
    }

    [@Fact]
    public void Equal_Methods_Are_Equal()
    {
        Assert.True(comparer.Equals(T_M, T_M2));
    }

    [@Fact]
    public void Unequal_Methods_In_Same_Type_Are_unequal()
    {
        Assert.False(comparer.Equals(T_M, T_N));
        Assert.False(comparer.Equals(T_N, T_M));
    }

    [@Fact]
    public void Methods_In_Different_Types_Are_unequal()
    {
        Assert.False(comparer.Equals(T_M, U_M));
        Assert.False(comparer.Equals(U_M, T_M));
    }

    [@Fact]
    public void Type_Equals_Method_In_It()
    {
        Assert.True(comparer.Equals(TypeT, T_M));
        Assert.True(comparer.Equals(T_M, TypeT));
    }
    [@Fact]
    public void Type_Does_Not_Equal_Method_In_Other_Type()
    {
        Assert.False(comparer.Equals(TypeT, U_M));
        Assert.False(comparer.Equals(U_M, TypeT));
    }

}
