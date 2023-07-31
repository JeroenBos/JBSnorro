using JBSnorro.Diagnostics;
using JBSnorro.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Text;

[TestClass]
public class LanguageStringVerifierTests
{
	[TestMethod]
	public void TestValidEscapeCharacters()
	{
		Contract.Assert("".ContainsOnlyValidEscapeSequences());
		Contract.Assert("\n".ContainsOnlyValidEscapeSequences());
		Contract.Assert("\\n".ContainsOnlyValidEscapeSequences());
		Contract.Assert("\\u1234".ContainsOnlyValidEscapeSequences());
		Contract.Assert("\\c".ContainsOnlyValidEscapeSequences() is false);
		Contract.Assert("\\".ContainsOnlyValidEscapeSequences() is false);
		Contract.Assert("//".ContainsOnlyValidEscapeSequences());
	}

	[TestMethod]
	public void TestContainsOnlyValidEscapeSequencesByExample()
	{
		const string validString = @"C:\\git\\BlaTeX\\wwwroot\\js\\blatex_wrapper.js";
		Contract.Assert(validString.ContainsOnlyValidEscapeSequences());
	}
}
