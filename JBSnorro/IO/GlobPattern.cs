using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JBSnorro.Extensions;

namespace JBSnorro;

public class GlobPattern
{
    public static GlobPattern SubdirectoriesPattern { get; } = new GlobPattern("**/");

    // invariant for directories: no string in this list contains ** except when it is equal to **
    private readonly IReadOnlyList<string> directories;
    // if filename is null, that means directories are being matched. You cannot match both (not implemented).
    private readonly string? filename;
    private readonly bool caseSensitive;
    public GlobPattern(string pattern, bool? caseSensitive = null)
    {
        (this.directories, this.filename) = SplitByDirectorySeparatorsAndDirectoryWildcard(pattern);
        this.caseSensitive = caseSensitive ?? Global.IsFileSystemCaseSensitive;
    }
    private GlobPattern(IReadOnlyList<string> directories, string? filename, bool caseSensitive)
    {
        this.directories = directories;
        this.filename = filename;
        this.caseSensitive = caseSensitive;
    }


    /// <summary>
    /// Returns whether the path (relative to the patterns) matches the patterns.
    /// The patterns support the following wildcards:
    /// - ? for single character wildcard
    /// - * for multiple character wildcard
    /// - ** for nested directories
    /// 
    /// Other properties of this matching algorithm:
    /// - Patterns ending in either directory separator character match directories only, not files.
    /// - ** is allowed at most once per pattern. _could_ be supported, is not though.
    /// - ** at the end of file names essentially turns it in a directory constraint, e.g.
    ///   - dir**.txt is equivalent to dir*/**/*.txt, meaning all .txt files in directories starting with dir or, recursively, subdirectories thereof.
    ///   - dir/**.txt is equivalent to dir/**/*.txt
    ///   - **.txt means all .txt files in all (sub)directories
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="patterns"></param>
    /// <param name="ignorepatterns"></param>
    /// <returns></returns>
    public static bool Matches(string relativePath, IReadOnlyList<string> patterns, IReadOnlyList<string> ignorepatterns)
    {
        // relativePath is relative to sourcePath, and so are the patterns
        var patternObjs = patterns.Select(pattern => new GlobPattern(pattern));
        if (!patternObjs.Any(pattern => pattern.Matches(relativePath)))
        {
            return false;
        }

        var ignorePatternObjs = ignorepatterns.Select(pattern => new GlobPattern(pattern)).ToList();
        if (ignorePatternObjs.Any(pattern => pattern.Matches(relativePath)))
        {
            return false;
        }
        return true;
    }
    internal static (IReadOnlyList<string> Subdirectories, string? filename) SplitByDirectorySeparatorsAndDirectoryWildcard(string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.ContainsMultiple("**"))
            throw new ArgumentException("Contains multiple **", nameof(path));

        path = path.Trim();
        var segments = path.SplitByDirectorySeparators().ToList();
        if (segments.Count == 0)
            return (Array.Empty<string>(), null);

        string? filename = null;
        if (!path.EndsWith(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            filename = segments.Last();
            segments.RemoveLast();

            // split the ** off from the directory
            int i = filename.IndexOf("**");
            if (i != -1)
            {
                segments.Add(filename[..i]);
                filename = filename[(i + "*".Length)..]; // e.g. dir**.txt -> *.txt
                if (i == (^2).GetOffset(filename.Length))
                {
                    // if the pattern ends on **, that's any (nested) file,
                    // as opposed to ending on **/, which is any (nested) directory
                }
                if (i == 0)
                {
                    if (segments[^1] == "")
                    {
                        segments[^1] = "**"; // the corner case where **.txt -> **/*.txt
                    }
                    else
                    {
                        segments[^1] += "*"; // e.g. dir**.txt -> dir*/
                    }
                }
            }
        }

        static IEnumerable<string> splitByDirectoryWildcard(string segment)
        {
            int i = segment.IndexOf("**");
            if (i == -1)
            {
                yield return segment;
                yield break;
            }

            // yield only the non-empty parts
            if (i != 0)
                yield return segment[..i];

            yield return "**";

            if (i != segment.Length - 2)
                yield return segment[(i + 2)..];
        }

        var normalizedSegments = segments.SelectMany(splitByDirectoryWildcard).ToList();

        return (normalizedSegments, filename);
    }

    public bool Matches(string relativePath)
    {
        var (segments, filename) = SplitByDirectorySeparatorsAndDirectoryWildcard(relativePath);
        return Matches(segments, filename);
    }
    public bool Matches(FileSystemInfo fileOrDir, string relativeRoot)
    {
        var relativePath = Path.GetRelativePath(relativeRoot, fileOrDir.ToString());
        return Matches(relativePath);
    }
    internal bool Matches(IReadOnlyList<string> pathSegments, string? filename)
    {
        return MatchesDirectories(pathSegments) && MatchesFilename(filename);
    }
    internal bool MatchesDirectories(IReadOnlyList<string> directories)
    {

        foreach (var (patternDir, dir) in this.directories.Zip(directories))
        {
            if (patternDir == "**")
                goto fromEnd;

            if (!FileSystemName.MatchesSimpleExpression(patternDir, dir, ignoreCase: !this.caseSensitive))
                return false;
        }

        // if the next directory is **
        if (this.directories.Count  > directories.Count && this.directories[directories.Count] == "**")
            goto fromEnd;

        // no ** encountered
        return this.directories.Count == directories.Count;


    fromEnd:
        foreach (var (patternDir, dir) in this.directories.Reverse().Zip(directories.Reverse()))
        {
            if (patternDir == "**")
            {
                return true;
            }

            if (!FileSystemName.MatchesSimpleExpression(patternDir, dir, ignoreCase: !this.caseSensitive))
                return false;
        }

        if (this.directories.Count == 0 || directories.Count != 0 || this.directories[0] != "**")
            throw new Exception("Not reachable");

        return true;
    }
    internal bool MatchesFilename(string? filename)
    {
        if (ReferenceEquals(filename, this.filename))
            return true;

        if ((filename == null) || (this.filename == null))
            return false;

        return FileSystemName.MatchesSimpleExpression(this.filename, filename, ignoreCase: !this.caseSensitive);
    }
    /// <summary>
    /// Gets a new <see cref="GlobPattern"/> for a subdirectory; or <see langword="null"/> if this pattern does not apply.
    /// </summary>
    /// <param name="segment">The name of the subdirectory to create the pattern for. </param>
    public GlobPattern? ForSubdirectory(string segment)
    {
        if (this.filename != null)
        {
            return null;
        }

        if (this.directories.Count == 0)
        {
            // ?
            throw new NotFiniteNumberException("directories.Count == 0. Not sure how this could occur");
        }
        else if (this.directories[0] == "**")
        {
            return this;
        }
        else if (FileSystemName.MatchesSimpleExpression(this.directories[0], segment, ignoreCase: !this.caseSensitive))
        {
            return new GlobPattern(this.directories.Skip(1).ToArray(), this.filename, this.caseSensitive);
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// Gets whether the current glob pattern can match anything in the specified subdirectory.
    /// </summary>
    public bool MatchesSubdirectory(string segment)
    {
        return ForSubdirectory(segment) != null;
    }
}
