using System;
using System.Collections.Generic;
using System.Text;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;

namespace JBSnorro.Text
{
	/*
		The complete set of escape sequences in C# is as follows:

		\' - single quote, needed for character literals
		\" - double quote, needed for string literals
		\\ - backslash
		\0 - Unicode character 0
		\a - Alert(character 7)
		\b - Backspace(character 8)
		\f - Form feed(character 12)
		\n - New line(character 10)
		\r - Carriage return (character 13)
		\t - Horizontal tab(character 9)
		\v - Vertical tab(character 11)
		\uxxxx - Unicode escape sequence for character with hex value xxxx
		\xn[n][n][n] - Unicode escape sequence for character with hex value nnnn(variable length version of \uxxxx)
		\Uxxxxxxxx - Unicode escape sequence for character with hex value xxxxxxxx(for generating surrogates)
	*/

	public static class LanguageStringVerifier
	{
		/// <summary>
		/// Verifies that if the string were used verbatim in the language JS or C#, that they would be valid.
		/// Invalid could for instance be "\c" because an escaped 'c' does not exist, as opposed to e.g. 'n', 't'. 
		/// </summary>
		public static bool ContainsOnlyValidEscapeSequences(this string s)
		{
			return s.AsSpan().ContainsOnlyValidEscapeSequences();
		}
		/// <summary>
		/// Verifies that if the string were used verbatim in the language JS or C#, that they would be valid.
		/// Invalid could for instance be "\c" because an escaped 'c' does not exist, as opposed to e.g. 'n', 't'.
		/// </summary>
		public static bool ContainsOnlyValidEscapeSequences(this ReadOnlySpan<char> s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '\\')
				{
					if (!s[(i + 1)..].StartsWithValidEscapeSequence(out int escapedLength))
					{
						return false;
					}
					i += escapedLength;
				}
			}
			return true;
		}
		public static bool StartsWithValidEscapeSequence(this string s)
		{
			return s.AsSpan().StartsWithValidEscapeSequence();
		}
		public static bool StartsWithValidEscapeSequence(this string s, out int length)
		{
			return s.AsSpan().StartsWithValidEscapeSequence(out length);
		}
		public static bool StartsWithValidEscapeSequence(this ReadOnlySpan<char> s)
		{
			return s.StartsWithValidEscapeSequence(out var _);
		}
		public static bool StartsWithValidEscapeSequence(this ReadOnlySpan<char> s, out int length)
		{

			length = 0;
			if (s.Length == 0)
				return false;

			switch (s[0])
			{
				case '\'':
				case '"':
				case '\\':
				case '0':
				case 'a':
				case 'b':
				case 'f':
				case 'n':
				case 'r':
				case 't':
				case 'v':
					length = 1;
					return true;
				case 'u':
					{
						if (s.Length < 5)
							return false;
						for (int i = 1; i < 5; i++)
						{
							if (!s[i].IsHexNumber())
								return false;
						}
						length = 5;
						return true;
					}
				case 'x':
					{
						if (s.Length < 5)
							return false;
						if (!s[1].IsHexNumber())
							return false;
						for (length = 2; length < 5; length++)
						{
							if (!s[length].IsHexNumber())
								break;
						}
						return true;
					}
				case 'U':
					{
						if (s.Length < 9)
							return false;
						for (int i = 0; i < 9; i++)
						{
							if (!s[i].IsHexNumber())
								return false;
						}
						length = 9;
						return true;
					}
				default:
					return false;
			}
		}
	}

	public class LanguageString
	{
		/// <summary>
		/// Gets the value of the string in 
		/// </summary>
		public string Value { get; }
		/// <summary>
		/// Gets whether single quotes are escaped (i.e. they should be in C# character literals and JS strings).
		/// </summary>
		public bool SingleQuotesEscaped { get; }
		/// <summary>
		/// Gets whether double quotes are escaped (i.e. they should be in C# characstringter literals and JS strings).
		/// </summary>
		public bool DoubleQuotesEscaped { get; }

		public LanguageString(string value, bool singleQuotesEscaped, bool doubleQuotesEscaped)
		{
			Contract.Requires(value != null);
			Contract.Requires(value.ContainsOnlyValidEscapeSequences(), "Contains invalid escape characters");

			this.Value = value;
			this.SingleQuotesEscaped = singleQuotesEscaped;
			this.DoubleQuotesEscaped = doubleQuotesEscaped;
		}

		/// <summary>
		/// Converts the specified string to a string as it would appear in C# source code in a character literal.
		/// </summary>
		public static LanguageString EscapeCharacterLiteral(string s) => Escape(s, escapeSingleQuotes: true, escapeDoubleQuotes: false);
		/// <summary>
		/// Converts the specified string to a string as it would appear in C# source code in a string literal.
		/// </summary>
		public static LanguageString EscapeCSharpString(string s) => Escape(s, escapeSingleQuotes: false, escapeDoubleQuotes: true);
		/// <summary>
		/// Converts the specified string to a string as it would appear in JS source code.
		/// </summary>
		public static JSString EscapeJSString(string value) => JSString.Escape(value);
		/// <summary>
		/// Converts the specified string to a JS/C# source code string.
		/// </summary>
		public static LanguageString Escape(string s, bool escapeSingleQuotes, bool escapeDoubleQuotes)
		{
			return new LanguageString(escape(s, escapeSingleQuotes, escapeDoubleQuotes), escapeSingleQuotes, escapeDoubleQuotes);
		}
		/// <summary>
		/// Converts the specified string to a JS/C# source code string.
		/// </summary>
		internal static string escape(string s, bool escapeSingleQuotes, bool escapeDoubleQuotes)
		{
			Contract.Requires(s != null);
			string result = s.Replace("\\", "\\\\")
							 .Replace("\0", "\\0")
							 .Replace("\a", "\\a")
							 .Replace("\b", "\\b")
							 .Replace("\f", "\\f")
							 .Replace("\n", "\\n")
							 .Replace("\r", "\\r")
							 .Replace("\t", "\\t")
							 .Replace("\v", "\\v");

			if (escapeSingleQuotes)
			{
				result = result.Replace("'", "\\'");
			}
			if (escapeDoubleQuotes)
			{
				result = result.Replace("\"", "\\\"");
			}

			// TODO: unicode not implemented yet

			return result;
		}


		public static implicit operator string(LanguageString s) => s.Value;
	}

	public class JSString : LanguageString
	{
		public static JSString Escape(string value)
		{
			return new JSString(escape(value, escapeSingleQuotes: true, escapeDoubleQuotes: true));
		}
		internal JSString(string value) : base(value, true, true) { }

		public static implicit operator string(JSString s) => s.Value;
	}
}
