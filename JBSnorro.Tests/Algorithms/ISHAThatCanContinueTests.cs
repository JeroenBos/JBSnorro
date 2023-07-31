using JBSnorro.Algorithms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Algorithms;

[TestClass]
public class ISHAThatCanContinueTests
{
    [TestMethod]    
    public void BytesInOneGoHasTheSameSHAAsInTwoGoes()
    {
        var bytes = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();

        var expected = ISHAThatCanContinue.CreateOneShot().AppendFinalHashData(bytes);

        var multiple = ISHAThatCanContinue.Create();
        multiple.AppendHashData(bytes.AsSpan()[..100]);
        var result = multiple.AppendFinalHashData(bytes.AsSpan()[100..]);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void CallingToStringImplicitlyFinishes()
    {
        var bytes = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();

        var expected = ISHAThatCanContinue.CreateOneShot().AppendFinalHashData(bytes);

        var multiple = ISHAThatCanContinue.Create();
        multiple.AppendHashData(bytes.AsSpan()[..100]);
        multiple.AppendHashData(bytes.AsSpan()[100..]);
        var result = multiple.ToString();

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void IsNotIdempotent()
    {
        var bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

        var notExpected = ISHAThatCanContinue.CreateOneShot().AppendFinalHashData(bytes);

        var multiple = ISHAThatCanContinue.Create();
        multiple.AppendHashData(bytes.AsSpan()[..100]);
        multiple.AppendHashData(bytes.AsSpan()[..100]);
        var result = multiple.ToString();

        Assert.AreNotEqual(notExpected, result);
    }
}
