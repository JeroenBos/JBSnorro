using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CrashTest
{
    [TestMethod]
    public void PurposefullyCrashingCI()
    {
        throw new Exception();
    }
}
