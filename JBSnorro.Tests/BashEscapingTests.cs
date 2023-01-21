#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static JBSnorro.Diagnostics.Contract;

namespace JBSnorro.Tests;

[TestClass]
public class BashEscapingTests
{
    [TestMethod]
    public void EscapeEmpty()
    {
        Assert(ProcessExtensions.BashEscape("") == "");
    }
    [TestMethod]
    public void EscapeA()
    {
        Assert(ProcessExtensions.BashEscape("A") == "A");
    }
    [TestMethod]
    public void EscapeDoubleQuote()
    {
        string result = ProcessExtensions.BashEscape("\"");
        Assert(result == "\\\"");
    }
    [TestMethod]
    public void EscapeTwoDoubleQuotes()
    {
        string result = ProcessExtensions.BashEscape("\"\"");
        Assert(result == "\\\"\\\"");
    }
    [TestMethod]
    public void EscapeSlash()
    {
        string result = ProcessExtensions.BashEscape("\\");
        Assert(result == "\\");
    }
    [TestMethod]
    public void EscapeTwoSlashes()
    {
        string result = ProcessExtensions.BashEscape("\\\\");
        Assert(result == "\\\\");
    }
    [TestMethod]
    public void EscapeTwoSlashesAndA()
    {
        string result = ProcessExtensions.BashEscape("\\\\a");
        Assert(result == "\\\\a");
    }
    [TestMethod]
    public void EscapeTwoSlashesAndB()
    {
        string result = ProcessExtensions.BashEscape("\\\\b");
        Assert(result == "\\\\b");
    }
    [TestMethod]
    public void EscapeSlashAndDoubleQuote()
    {
        string result = ProcessExtensions.BashEscape("\\\"");
        Assert(result == "\\\\\\\"");
    }
    [TestMethod]
    public void EscapeDoubleQuoteAndSlash()
    {
        string result = ProcessExtensions.BashEscape("\"\\");
        Assert(result == "\\\"\\");
    }
    [TestMethod]
    public void StringTabBash()
    {
        string tab = ProcessExtensions.BashEscape("\\t");
        Assert(tab == "\\t");

        string result = ProcessExtensions.BashEscape("\"\\t\"");
        Assert(result == "\\\"\\t\\\"");

        // literals JS:
        // console.log("\t")
        string example = ProcessExtensions.BashEscape("console.log(\"\\t\")");
        Assert(example == "console.log(\\\"\\t\\\")");
    }
}
