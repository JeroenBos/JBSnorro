using JBSnorro.JS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static JBSnorro.Diagnostics.Contract;

namespace Tests.JBSnorro.JS;

[TestClass]
public class BashEscapingTests
{
    [TestMethod]
    public void EscapeEmpty()
    {
        Assert(JSProcessRunner.BashEscape("") == "");
    }
    [TestMethod]
    public void EscapeA()
    {
        Assert(JSProcessRunner.BashEscape("A") == "A");
    }
    [TestMethod]
    public void EscapeDoubleQuote()
    {
        string result = JSProcessRunner.BashEscape("\"");
        Assert(result == "\\\"");
    }
    [TestMethod]
    public void EscapeTwoDoubleQuotes()
    {
        string result = JSProcessRunner.BashEscape("\"\"");
        Assert(result == "\\\"\\\"");
    }
    [TestMethod]
    public void EscapeSlash()
    {
        string result = JSProcessRunner.BashEscape("\\");
        Assert(result == "\\");
    }
    [TestMethod]
    public void EscapeTwoSlashes()
    {
        string result = JSProcessRunner.BashEscape("\\\\");
        Assert(result == "\\\\");
    }
    [TestMethod]
    public void EscapeTwoSlashesAndA()
    {
        string result = JSProcessRunner.BashEscape("\\\\a");
        Assert(result == "\\\\a");
    }
    [TestMethod]
    public void EscapeTwoSlashesAndB()
    {
        string result = JSProcessRunner.BashEscape("\\\\b");
        Assert(result == "\\\\b");
    }
    [TestMethod]
    public void EscapeSlashAndDoubleQuote()
    {
        string result = JSProcessRunner.BashEscape("\\\"");
        Assert(result == "\\\\\\\"");
    }
    [TestMethod]
    public void EscapeDoubleQuoteAndSlash()
    {
        string result = JSProcessRunner.BashEscape("\"\\");
        Assert(result == "\\\"\\");
    }
    [TestMethod]
    public void StringTabBash()
    {
        string tab = JSProcessRunner.BashEscape("\\t");
        Assert(tab == "\\t");

        string result = JSProcessRunner.BashEscape("\"\\t\"");
        Assert(result == "\\\"\\t\\\"");

        // literals JS:
        // console.log("\t")
        string example = JSProcessRunner.BashEscape("console.log(\"\\t\")");
        Assert(example == "console.log(\\\"\\t\\\")");
    }
}
