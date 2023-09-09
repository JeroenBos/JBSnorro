using JBSnorro.Diagnostics;
using JBSnorro.Text;
using System.Diagnostics;
using System.Globalization;

namespace JBSnorro.Extensions;

public static class StringExtensions
{
    /// <summary> Returns whether this string compares alphabetically to the specified enumerable of chars. </summary>
    /// <param name="s"> The string to compare with the characters. </param>
    /// <param name="chars"> The characters to regard as string and compare with the other string. </param>
    /// <returns> a negative number if the string compares smaller than the characters, that is, the first unequal character is smaller in the string, or the string has less characters, whichever happens first. 
    /// a positive number is returned in the opposite case, and 0 is returned only if the arguments are equal, character-wise. </returns>
    public static int CompareTo(this string s, IEnumerable<char> chars)
    {
        int i = 0;
        foreach (char c in chars)
        {
            if (s.Length == i) // if chars is longer than s
            {
                return -1;
            }

            int comparisonResult = s[i].CompareTo(c);
            if (comparisonResult != 0)
            {
                return comparisonResult;
            }
            i++;
        }
        return s.Length == i ? 0 : 1;
    }
    /// <summary> Returns the reverse of the specified string. </summary>
    /// <param name="characters"> The characters to reverse. </param>
    public static string Reverse(this IEnumerable<char> characters)
    {
        Contract.Requires(characters != null);

        string? asString = characters as string;
        if (asString == null)
        {
            char[] asCharArray = (characters as char[]) ?? characters.ToArray();
            asString = new string(asCharArray);
        }
        return Reverse(asString);
    }
    /// <summary> Returns the reverse of the specified string. </summary>
    /// <param name="s"> The string to reverse. </param>
    public static string Reverse(this string s)
    {
        Contract.Requires(s != null);

        var cache = new List<string>(s.Length);
        TextElementEnumerator e = StringInfo.GetTextElementEnumerator(s);
        while (e.MoveNext())
        {
            cache.Add(e.GetTextElement());
        }

        cache.Reverse();
        string result = string.Concat(cache);
        return result;
    }
    /// <summary> Determines whether a strings starts with the specified sequence of characters. </summary>
    /// <param name="source"> The string to check whether its starts with <paramref name="characters"/>. </param>
    /// <param name="characters"> The sequence of characters to look for in <paramref name="source"/>. </param>
    public static bool StartsWith(this string source, IEnumerable<char> characters)
    {
        return source.StartsWith(characters, EqualityComparer<char>.Default.Equals);
    }


    public static string ToFirstLower(this string s)
    {
        Contract.Requires(!string.IsNullOrEmpty(s));

        char lower = char.ToLowerInvariant(s[0]);
        if (s[0] == lower)
            return s;
        string result = lower.ToString() + s.Substring(1);
        Contract.Ensures(result.Length == s.Length);
        Contract.Ensures(result[0] == char.ToLowerInvariant(s[0]));
        Contract.Ensures(result.Skip(1).SequenceEqual(s.Skip(1)));
        return result;
    }
    public static string ToFirstUpper(this string s)
    {
        Contract.Requires(!string.IsNullOrEmpty(s));

        char upper = char.ToUpperInvariant(s[0]);
        if (s[0] == upper)
            return s;
        string result = upper.ToString() + s.Substring(1);
        Contract.Ensures(result.Length == s.Length);
        Contract.Ensures(result[0] == char.ToUpperInvariant(s[0]));
        Contract.Ensures(result.Skip(1).SequenceEqual(s.Skip(1)));
        return result;
    }

    [DebuggerHidden]
    public static string Join(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
    [DebuggerHidden]
    public static string Join<T>(this IEnumerable<T> strings, string separator) => string.Join(separator, strings);
    /// <summary> Gets all indices of the specified item in the specified string. </summary>
    [DebuggerHidden]
    public static IEnumerable<int> IndicesOf(this string s,
                                             string item,
                                             StringComparison comparison = StringComparison.CurrentCulture)
    {
        Contract.Requires(s != null);
        Contract.Requires(item != null);

        for (int i = s.IndexOf(item, comparison); i != -1; i = s.IndexOf(item, i + 1, comparison))
        {
            yield return i;
        }
    }
    /// <summary> Gets whether the specified string is at the specified index in <paramref name="s"/>. </summary>
    public static bool EqualsAt(this string s,
                                string item,
                                int index,
                                IEqualityComparer<string>? equalityComparer = null)
    {
        Contract.Requires(s != null);
        Contract.Requires(!string.IsNullOrEmpty(item));

        if (index + item.Length > s.Length)
            return false;

        var markupFragment = s.Substring(index, item.Length);

        equalityComparer ??= EqualityComparer<string>.Default;
        bool result = equalityComparer.Equals(markupFragment, item);
        return result;
    }

    /// <summary> 
    /// Gets the index of the character after the first occurrence of <paramref name="value"/>;
    /// or -1 is the value was not found. 
    /// </summary>
    public static int IndexAfter(this string s, string value, int startIndex = 0)
    {
        int i = s.IndexOf(value, startIndex);
        if (i == -1)
            return -1;
        return i + value.Length;
    }
    /// <summary> 
    /// Gets the substring of <paramref name="s"/> after the first occurrence of <paramref name="value"/>;
    /// or <paramref name="s"/> if the value was not.
    /// </summary>
    public static string SubstringAfter(this string s, string value, int startIndex = 0)
    {
        int i = s.IndexAfter(value, startIndex);
        if (i == -1)
            return s;
        return s[i..];
    }
    /// <inheritdoc cref="SubstringAfterLast(string, string, Index)"/>
    public static string SubstringAfterLast(this string s, string value)
    {
        return s.SubstringAfterLast(value, Index.End);
    }
    /// <summary> 
    /// Gets the substring of <paramref name="this"/> after the last occurrence of <paramref name="value"/>;
    /// or <paramref name="this"/> if the value was not.
    /// </summary>
    /// <param name="value"> The value to find. </param>
    /// <param name="startIndex"> The search starting position. The search proceeds from startIndex toward the beginning of this instance. </param>
    public static string SubstringAfterLast(this string @this, string value, Index startIndex)
    {
        int i = @this.LastIndexOf(value, startIndex.GetOffset(@this.Length));
        if (i == -1)
            return @this;
        return @this.Substring(i + value.Length);
    }
    /// <summary>
    /// Gets the substring of <paramref name="this"/> until the first occurrence of <paramref name="value"/>;
    /// or <paramref name="this"/> if the value was not.
    /// </summary>
    public static string SubstringUntil(this string @this, string value, int startIndex = 0, bool includeValue = false)
    {
        int i = includeValue ? @this.IndexAfter(value, startIndex) : @this.IndexOf(value, startIndex);
        if (i == -1)
            return @this;
        return @this[..i];
    }
    /// <summary> 
    /// Gets the substring of <paramref name="this"/> until the last occurrence of <paramref name="value"/>;
    /// or <paramref name="this"/> if the value was not.
    /// </summary>
    /// <param name="value"> The value to find. </param>
    public static string SubstringUntilLast(this string @this, string value)
    {
        return SubstringUntilLast(@this, value, Index.End);
    }
    /// <summary> 
    /// Gets the substring of <paramref name="this"/> until the last occurrence of <paramref name="value"/>;
    /// or <paramref name="this"/> if the value was not.
    /// </summary>
    /// <param name="value"> The value to find. </param>
    /// <param name="startIndex"> The search starting position. The search proceeds from startIndex toward the beginning of this instance. </param>
    public static string SubstringUntilLast(this string @this, string value, Index startIndex)
    {
        int i = @this.LastIndexOf(value, startIndex.GetOffset(@this.Length));
        if (i == -1)
            return @this;
        return @this[..i];
    }
    /// <summary>
    /// Gets the index of the specified value in the string, searching up to <paramref name="endIndex"/>.
    /// </summary>
    public static int LastIndexOf(this string @this, string value, int endIndex)
    {
        int result = @this.LastIndexOf(value, endIndex);
        if (result + value.Length > endIndex)
            return -1;
        return result;
    }

    /// <summary>
    /// Gets the first index in <paramref name="this"/> string of <paramref name="value"/> that is not preceded by <paramref name="notPrecededByValue"/>.
    /// </summary>
    public static int IndexOfNotPrecededBy(this string @this, string value, string notPrecededByValue, int startIndex = 0)
    {
        int i = startIndex - 1;
        while (true)
        {
            i = @this.IndexOf(value, i + 1);
            if (i == -1)
                return -1;

            if (i >= notPrecededByValue.Length && !@this.EqualsAt(notPrecededByValue, i - notPrecededByValue.Length))
                return i;
        }
    }

    /// <summary>
    /// Gets whether the specified character is a letter, digit or underscore.
    /// </summary>
    public static bool IsLetterOrDigitOrUnderscore(this char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
    public static bool IsHexNumber(this char c)
    {
        return char.IsDigit(c) || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
    }

    /// <summary>
    /// Gets the string in <paramref name="s"/> between <paramref name="start"/> and <paramref name="end"/>.
    /// </summary>
    public static string? Substring(this string s, string start, string? end = null, int startIndex = 0)
    {
        Contract.Requires(s != null, nameof(s));
        Contract.Requires(start != null, nameof(start));
        Contract.Requires(end != null, nameof(end));

        int i = s.IndexOf(start, startIndex);
        if (i == -1)
            return null;

        if (end == null)
            return s[i..];

        int endIndex = s.IndexOf(end, i + start.Length);
        if (endIndex == -1)
            return null;

        return s[i..endIndex];
    }

    /// <summary>
    /// Imitates Python's
    /// <a href="https://docs.python.org/3/library/textwrap.html#textwrap.dedent">
    /// <c>textwrap.dedent</c></a>.
    /// </summary>
    public static string Dedent(this string text)
    {
        var builder = new ConfigurableStringBuilder();
        foreach (var line in text.DedentToLines())
            builder.AppendLine(line);
        return builder.ToString();
    }
    /// <summary>
    /// Imitates Python's
    /// <a href="https://docs.python.org/3/library/textwrap.html#textwrap.dedent">
    /// <c>textwrap.dedent</c></a>.
    /// </summary>
    /// <param name="text">Text to be dedented</param>
    /// <returns>array of dedented lines</returns>
    /// <code doctest="true">
    /// Assert.That(Dedent(""), Is.EquivalentTo(new[] {""}));
    /// Assert.That(Dedent("test me"), Is.EquivalentTo(new[] {"test me"}));
    /// Assert.That(Dedent("test\nme"), Is.EquivalentTo(new[] {"test", "me"}));
    /// Assert.That(Dedent("test\n  me"), Is.EquivalentTo(new[] {"test", "  me"}));
    /// Assert.That(Dedent("test\n  me\n	again"), Is.EquivalentTo(new[] {"test", "me", "  again"}));
    /// Assert.That(Dedent("  test\n  me\n	again"), Is.EquivalentTo(new[] {"  test", "me", "  again"}));
    /// </code>
    /// <seealso href="https://stackoverflow.com/a/64934767/308451"/>
    public static string[] DedentToLines(this string text)
    {
        var lines = text.ToLines();

        // Search for the first non-empty line starting from the second line.
        // The first line is not expected to be indented.
        var firstNonemptyLine = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;

            firstNonemptyLine = i;
            break;
        }

        if (firstNonemptyLine < 0) return lines;

        // Search for the second non-empty line.
        // If there is no second non-empty line, we can return immediately as we
        // can not pin the indent.
        var secondNonemptyLine = -1;
        for (var i = firstNonemptyLine + 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;

            secondNonemptyLine = i;
            break;
        }

        if (secondNonemptyLine < 0) return lines;

        // Match the common prefix with at least two non-empty lines

        var firstNonemptyLineLength = lines[firstNonemptyLine].Length;
        var prefixLength = 0;

        for (int column = 0; column < firstNonemptyLineLength; column++)
        {
            char c = lines[firstNonemptyLine][column];
            if (c != ' ' && c != '\t') break;

            bool matched = true;
            for (int lineIdx = firstNonemptyLine + 1; lineIdx < lines.Length;
                    lineIdx++)
            {
                if (lines[lineIdx].Length == 0) continue;

                if (lines[lineIdx].Length < column + 1)
                {
                    matched = false;
                    break;
                }

                if (lines[lineIdx][column] != c)
                {
                    matched = false;
                    break;
                }
            }

            if (!matched) break;

            prefixLength++;
        }

        if (prefixLength == 0) return lines;

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length > 0) lines[i] = lines[i].Substring(prefixLength);
        }

        return lines;
    }
    /// <summary>
    /// Splits the string by all newline characters.
    /// </summary>
    public static string[] ToLines(this string s)
    {
        return s.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }
    /// <summary>
    /// Returns the string enclosed by double quotes on each side.
    /// </summary>
    public static string WrapInDoubleQuotes(this string s) => s.WrapIn("\"");
    /// <summary>
    /// Returns the string enclosed by single quotes on each side.
    /// </summary>
    public static string WrapInSingleQuotes(this string s) => s.WrapIn("'");
    /// <summary>
    /// Returns the string enclosed by <paramref name="enclosing"/> on each side.
    /// </summary>
    public static string WrapIn(this string s, string enclosing) => enclosing + s + enclosing;

    /// <summary>
    /// Inserts the specified string into the string at the specified index, removing <paramref name="length"/> characters.
    /// </summary>
    public static string ReplaceAt(this string s, int index, int length, string item)
    {
        return s.Substring(0, index) + item + s.Substring(index + length);
    }

    private static readonly char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>
    /// Splits the specified string by directory separator characters.
    /// </summary>
    public static IEnumerable<string> SplitByDirectorySeparators(this string s)
    {
        return s.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
    }
    /// <summary>
    /// Returns the path appended with a path separator if it doesn't end on one already.
    /// </summary>
    public static string EnsureEndsWithPathSeparator(this string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        if (path.EndsWith(DirectorySeparators))
        {
            return path;
        }
        else
        {
            return path + Path.DirectorySeparatorChar;
        }
    }
    /// <summary>
    /// If the path starts with `~`, it's replaced by the current user's home directory.
    /// </summary>
    public static string ExpandTildeAsHomeDir(this string path)
    {
        if (path.StartsWith("~"))
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        return path;
    }
    /// <summary>
    /// Returns whether the string ends with any of the specified items.
    /// </summary>
    public static bool EndsWith(this string s, params char[] items)
    {
        foreach (var item in items)
        {
            if (s.EndsWith(item))
                return true;
        }
        return false;
    }
    /// <summary>
    /// Gets whether the specified character is a directory separator characters (in any OS).
    /// </summary>
    public static bool IsDirectorySeparatorChar(this char c)
    {
        return DirectorySeparators.Contains(c);
    }
    /// <summary>
    /// Returns whether the item appears at least twice in the specified string.
    /// </summary>
    public static bool ContainsMultiple(this string s, string item)
    {
        int firstIndex = s.IndexOf(item);
        if (firstIndex == -1)
            return false;

        int secondIndex = s.IndexOf(item, firstIndex + 1);
        return secondIndex != -1;
    }
    /// <summary>
    /// Converts timespans to formats like 1h26m.
    /// </summary>
    public static string ToNiceString(this TimeSpan span, int remainingSignificantParts = 1, int millisecondsPrecision = 0)
    {
        if (millisecondsPrecision < 0 || millisecondsPrecision > 9) throw new ArgumentOutOfRangeException(nameof(millisecondsPrecision));
        if (millisecondsPrecision > 6) throw new NotImplementedException(nameof(millisecondsPrecision) + ". Need to implement nanoseconds");

        if (remainingSignificantParts <= 0)
            return "";

        if (span >= TimeSpan.FromDays(1))
        {
            int count = (int)span.TotalDays;
            var remaining = span - TimeSpan.FromDays(count);
            return $"{count}d{remaining.ToNiceString(remainingSignificantParts: count == 1 ? remainingSignificantParts : remainingSignificantParts - 1)}";
        }
        if (span >= TimeSpan.FromHours(1))
        {
            int count = (int)span.TotalHours;
            var remaining = span - TimeSpan.FromHours(count);
            return $"{count}h{remaining.ToNiceString(remainingSignificantParts: count == 1 ? remainingSignificantParts : remainingSignificantParts - 1)}";
        }
        if (span >= TimeSpan.FromMinutes(1))
        {
            int count = (int)span.TotalMinutes;
            var remaining = span - TimeSpan.FromMinutes(count);
            return $"{count}m{remaining.ToNiceString(remainingSignificantParts: count == 1 ? remainingSignificantParts : remainingSignificantParts - 1)}";
        }
        if (span >= TimeSpan.FromSeconds(1))
        {
            int count = (int)span.TotalSeconds;
            var remaining = span - TimeSpan.FromSeconds(count);
            return $"{count}s{remaining.ToNiceString(remainingSignificantParts: count < 10 ? remainingSignificantParts : remainingSignificantParts - 1)}";
        }
        if (millisecondsPrecision == 0)
            return "";
        return "." + span.TotalMilliseconds.ToString()[0..millisecondsPrecision];
    }
    public static string FormatToSignificantDigits(this float d, int significantDigitCount)
    {
        return ((double)d).FormatToSignificantDigits(significantDigitCount);
    }
    public static string FormatToSignificantDigits(this double d, int significantDigitCount)
    {
        if (double.IsNaN(d))
            return "NaN";
        if (double.IsInfinity(d))
        {
            if (double.IsPositiveInfinity(d))
                return "∞";
            else
                return "-∞";
        }
        int decimalPoints;
        if (Math.Abs(d) > 100_000)
        {
            decimalPoints = Math.Max(0, significantDigitCount - 5);
        }
        else if (Math.Abs(d) > 10_000)
        {
            decimalPoints = Math.Max(0, significantDigitCount - 4);
        }
        else if (Math.Abs(d) > 1_000)
        {
            decimalPoints = Math.Max(0, significantDigitCount - 3);
        }
        else if (Math.Abs(d) > 100)
        {
            decimalPoints = Math.Max(0, significantDigitCount - 2);
        }
        else if (Math.Abs(d) > 10)
        {
            decimalPoints = Math.Max(0, significantDigitCount - 1);
        }
        else
        {
            decimalPoints = significantDigitCount;
        }
        return d.ToString($"F{decimalPoints}");
    }
}
