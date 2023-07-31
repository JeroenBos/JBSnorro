using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Csx;

public static class GitUtilities
{
    internal static string ToBashOneliner(string s)
    {
        if (s.Contains("#"))
        {
            if (s.Split('\n').Any(line => line.Trim().StartsWith('#')))
                throw new ArgumentException("No comments allowed");
        }

        return s.Replace("\r\n", " ").Replace("\n", " ");
    }
    internal static NotImplementedException NotImplementedException(int exitCode, string stdOut, string stdErr, [CallerMemberName] string callerName = "")
    {
        if (exitCode == 0)
            return new NotImplementedException($"Unhandled output from '{callerName}': '{stdOut}'");
        if (!string.IsNullOrEmpty(stdErr))
            return new NotImplementedException($"ExitCode {exitCode} from '{callerName}', error output: '{stdErr}'");
        if (string.IsNullOrEmpty(stdOut))
            return new NotImplementedException($"ExitCode {exitCode} from '{callerName}', no standard nor error output");
        else
            return new NotImplementedException($"ExitCode {exitCode} from '{callerName}', no error output. Standard out: '{stdOut}'");
    }

    public static bool IsGitHubRunId(string s)
    {
        if (s == null)
            return false;
        if (s.Length != 10)
            return false;
        if (!s.All(char.IsDigit))
            return false;

        return true;
    }
    public static bool IsGitHash(string s)
    {
        if (s == null)
            return false;
        if (s.Length != 40)
            return false;
        if (!s.All(StringExtensions.IsHexNumber))
            return false;

        return true;
    }
    public static void AssertIsGitHash(string s, [CallerMemberName] string callerName = "")
    {
        string message = $"{callerName}: The string '{s}' is not a git hash";
        if (!IsGitHash(s))
        {
            throw new ContractException(message);
        }
    }

    public static bool IsValidBranchName(string name)
    {
        // following the spec from https://stackoverflow.com/a/3651867/308451
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Contains("/."))
            return false;
        if (name.EndsWith(".lock"))
            return false;
        if (name.Contains(".."))
            return false;
        if (name.Any(c => c < 32)) // '\040' in the SPEC is octal for 32
            return false;
        if (name.Any(" \t\u007F~^:?*[\\".Contains))
            return false;
        if (name.StartsWith('/') || name.EndsWith('/'))
            return false;
        if (name.Contains("//"))
            return false;
        if (name.EndsWith('.'))
            return false;
        if (name.Contains("@{"))
            return false;
        if (name == "@")
            return false;
        if (name.StartsWith('-'))
            return false;

        return true;
    }
}





[Serializable]
public class GitException : Exception
{
    public GitException() { }
    public GitException(string message) : base(message) { }
    public GitException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class NoInitialCommitGitException : GitException
{
    public NoInitialCommitGitException() : base("You do not have the initial commit yet") { }
    public NoInitialCommitGitException(string message) : base(message) { }
    public NoInitialCommitGitException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class StashEmptyGitException : GitException
{
    public StashEmptyGitException() : base("No stash entries found. ") { }
    public StashEmptyGitException(string message) : base(message) { }
    public StashEmptyGitException(string message, Exception inner) : base(message, inner) { }
}
[Serializable]
public class StashFailedBecauseOfUntrackedFiles : GitException
{
    public IReadOnlyList<string>? Filenames { get; init; }
    public StashFailedBecauseOfUntrackedFiles() : base("Stash not applied/popped because untracked files already exist. ") { }
    public StashFailedBecauseOfUntrackedFiles(string message) : base(message) { }
    public StashFailedBecauseOfUntrackedFiles(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class BranchAlreadyExistsGitException : GitException
{
    public BranchAlreadyExistsGitException() : base("Branch already exists. ") { }
    public BranchAlreadyExistsGitException(string message) : base(message) { }
    public BranchAlreadyExistsGitException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class AlreadyOnMainBranchGitException : GitException
{
    public AlreadyOnMainBranchGitException() : base("Already on main branch. ") { }
    public AlreadyOnMainBranchGitException(string message) : base(message) { }
    public AlreadyOnMainBranchGitException(string message, Exception inner) : base(message, inner) { }
}
[Serializable]
public class GitConflictException : GitException
{
    public GitConflictException() : base("A git conflict occurred. ") { }
    public GitConflictException(string message) : base("A git conflict occurred: " + message) { }
    public GitConflictException(string message, Exception inner) : base(message, inner) { }
}


class BaseRefNameResponse
{
    public string baseRefName { get; init; } = default!;
}
class HeadRefNameResponse
{
    public string headRefName { get; init; } = default!;
}
