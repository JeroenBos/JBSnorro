#nullable enable
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
    // is basically isomprophic to (IReadOnlyList<string> Patterns, IReadOnlyList<string> IgnorePatterns)
    public class GlobPatternCollection
    {
        private record NegatableGlobPattern
        {
            public GlobPattern Pattern { get; init; } = default!;
            public bool Negated { get; init; }
        }
        public static GlobPatternCollection Empty { get; } = new GlobPatternCollection(Array.Empty<GlobPattern>());
        public static GlobPatternCollection SubdirectoriesPattern { get; } = new GlobPatternCollection(new[] { GlobPattern.SubdirectoriesPattern });

        private readonly IReadOnlyList<NegatableGlobPattern> patterns;

        private GlobPatternCollection(IReadOnlyList<NegatableGlobPattern> patterns)
        {
            this.patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        }
        private GlobPatternCollection(IReadOnlyList<GlobPattern> patterns) : this(patterns?.Map(p => new NegatableGlobPattern() { Pattern = p })!)
        {
        }


        /// <summary>
        /// Reads a file and parses it as one glob-pattern per line.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="commentStartToken"></param>
        public static GlobPatternCollection FromFile(string path, string commentStartToken = "#")
        {
            var allLines = File.ReadAllLines(path);

            var lines = allLines.Select(line => line.SubstringUntil(commentStartToken))
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => line.Trim());

            return FromLines(lines);
        }
        public static GlobPatternCollection FromLines(IEnumerable<string> lines)
        {
            var patterns = new List<NegatableGlobPattern>();

            foreach (var line in lines)
            {
                if (line.StartsWith("!"))
                    patterns.Add(new NegatableGlobPattern
                    {
                        Pattern = new GlobPattern(line[1..]),
                        Negated = true,
                    });
                else
                    patterns.Add(new NegatableGlobPattern
                    {
                        Pattern = new GlobPattern(line),
                        Negated = false,
                    });
            }
            return new GlobPatternCollection(patterns);
        }

        /// <summary>
        /// Interprets a single string as a list (separated by <see cref="Path.PathSeparator"/>) of glob patterns and ignore glob patterns (those starting with !).
        /// </summary>
        public static GlobPatternCollection DecomposePatterns(string compositePatterns)
        {
            return FromLines(compositePatterns.Split(Path.PathSeparator));
        }


        public bool Matches(string relativePath)
        {
            foreach (var pattern in this.patterns.Reverse())
            {
                if (pattern.Pattern.Matches(relativePath))
                {
                    return !pattern.Negated;
                }
            }
            return false;
        }
        /// <summary>
        /// Gets whether the current glob pattern collection can match anything in the specified subfolder.
        /// </summary>
        public bool MatchesSubdirectory(string subdirectory)
        {
            foreach (var pattern in this.patterns.Reverse())
            {
                if (pattern.Pattern.MatchesSubdirectory(subdirectory))
                {
                    return !pattern.Negated;
                }
            }
            return false;
        }
        /// <summary>
        /// Gets a new <see cref="GlobPatternCollection"/> for a subdirectory; or the empty one if no pattern applies.
        /// </summary>
        /// <param name="segment">The name of the subdirectory to create the pattern for. </param>
        public GlobPatternCollection ForSubdirectory(string segment)
        {
            var subpatterns = this.patterns.Select(pattern => new NegatableGlobPattern { Pattern = pattern.Pattern.ForSubdirectory(segment)!, Negated = pattern.Negated, })
                                           .Where(pattern => pattern.Pattern != null)
                                           .ToList();

            if (subpatterns.Count == 0)
            {
                return Empty;
            }
            return new GlobPatternCollection(subpatterns);
        }
    }
}
