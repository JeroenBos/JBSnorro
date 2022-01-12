#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace JBSnorro.Csx
{
    public static class Git
    {
        public static string SSH_SCRIPT = "echo '' ";
        public static async Task<bool> IsDirty(string gitDir)
        {
            string bash = ToBashOneliner(@"
if [ -z ""$(git status --porcelain)"" ]; then 
    echo false;
else 
    echo true;
fi");
            var debug = "git status --porcelain".Execute(cwd: gitDir);
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);

            if (bool.TryParse(std_out, out bool result))
                return result;

            throw NotImplementedException(exitCode, std_out, std_err);
        }
        public static async Task<string> GetCurrentHash(string gitDir)
        {
            var (exitCode, std_out, std_err) = await "git rev-parse HEAD".Execute(cwd: gitDir);
            return std_out;
        }
        public static async Task<string?> GetCurrentBranch(string gitDir)
        {
            string bash = ToBashOneliner(@"
CURRENT_BRANCH_NAME=$(git rev-parse --symbolic-full-name --abbrev-ref HEAD);
if [ ""$CURRENT_BRANCH_NAME"" = HEAD ] ; then
    echo '';
else
    echo $CURRENT_BRANCH_NAME;
fi");


            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);

            if (std_out.Length == 0) // we're detached
                return null;
            if (!std_out.Contains('\n'))
                return std_out;

            throw NotImplementedException(exitCode, std_out, std_err);
        }
        public static async Task<string?> GetCurrentRemoteBranch(string gitDir)
        {
            string bash = "git rev-parse --abbrev-ref --symbolic-full-name @{u}";

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);

            if (std_err.StartsWith("fatal: no upstream configured for branch '"))
                return null;
            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, std_err);
            if (std_out.Length == 0) // we're detached
                return null;
            if (!std_out.Contains('\n'))
                return std_out;
            throw NotImplementedException(exitCode, std_out, std_err);
        }
        public static async Task<bool> GetBranchExists(string gitDir, string branch)
        {
            if (branch.StartsWith("origin/"))
                await Fetch(gitDir);

            string bash = $"git rev-parse --verify \"{branch}\"";
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);
            if (exitCode == 0 && std_out.Trim() != "")
                return true;
            if (std_err.StartsWith("fatal: Needed a single revision"))
                return false;
            else if (std_out.StartsWith("The system cannot find the path specified"))
            {
                Contract.Assert(Directory.Exists(gitDir), $"The git directory '{gitDir}' does not exist");
                Contract.Assert(false, $"The command '{bash}' failed in dir '{gitDir}'");
            }
            throw NotImplementedException(exitCode, std_out, std_err);
        }
        public static async Task<string> GetMainBranchName(string gitDir)
        {
            var masterExists = await GetBranchExists(gitDir, "master");
            var mainExists = GetBranchExists(gitDir, "main");

            if (masterExists)
                return "master";
            if (await mainExists)
                return "main";
            throw new InvalidOperationException("No master nor main branch found");

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
        public static async Task<bool> Stash(string gitDir, bool indexOnly = false)
        {
            string? bash = null;
            if (indexOnly)
            {
                var stagedFiles = await GetStagedFiles(gitDir);
                if (stagedFiles.Count != 0)
                {
                    // no need for --intent-to-add anymore because they're all staged anyway
                    bash = "git stash -- " + stagedFiles.Select(StringExtensions.WrapInSingleQuotes).Join(" ");
                }
            }
            if (bash == null)
            {
                await TrackAllUntrackedFiles(gitDir);

                // we want that because untracked files may disappear in the stash. If you don't believe me:
                // - stash 1 tracked and 1 untracked file
                // - pop it while the tracked file results in a conflict (for sure modified vs delete conflict)
                // - notice that the untracked file is not popped
                // - it's still in the stash entry, which wasn't dropped
                // - if you, like me, drop the stash entry after resolving all conflicts, then you would be deleting work forever, because git decided not to pop the untracked files 🤷🏻‍
                bash = "git stash"; // -u is harmful here
            }


            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);
            if (std_err.EndsWith("You do not have the initial commit yet"))
                throw new NoInitialCommitGitException();
            if (std_out.StartsWith("Saved working directory"))
                return true;
            if (std_out.StartsWith("No local changes to save"))
                return false;

            throw NotImplementedException(exitCode, std_out, std_err);
        }
        /// <param name="force"> If the files cannot be overwritten (because they are untracked) if true, it will be done anyway. </param>
        public static async Task PopStash(string gitDir, bool force = false, bool throwOnConflict = false)
        {
            string bash = "git stash pop";

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);
            if (std_err.StartsWith("No stash entries found"))
            {
                throw new StashEmptyGitException();
            }

            const string alreadyExists = "already exists, no checkout";
            if (std_err.Contains(alreadyExists))
            {
                Contract.Assert(!std_err.Contains('\r'), "Windows linebreak detected");
                var filenames = std_err.Split('\n')
                                       .Where(line => line.EndsWith(alreadyExists))
                                       .Select(line => line[..^alreadyExists.Length])
                                       .ToList();
                if (force)
                {
                    var tempDir = IOExtensions.CreateTemporaryDirectory();
                    Console.WriteLine($"Moving untracked to-be-overwritten files to temp path '{tempDir}'");
                    foreach (var filename in filenames)
                    {
                        try
                        {
                            string source = Path.Combine(gitDir, filename);
                            string dest = Path.Combine(tempDir, filename);

                            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                            File.Move(source, dest, overwrite: true);
                        }
                        catch (IOException)
                        {
                            // nothing to do here if it fails. The next recursive call simply a StashFailedBecauseOfUntrackedFiles will be thrown.
                        }
                    }
                    await PopStash(gitDir, force: false, throwOnConflict);
                }
                else
                {
                    throw new StashFailedBecauseOfUntrackedFiles() { Filenames = filenames };
                }
            }
            if (exitCode == 1 && std_out.StartsWith("CONFLICT"))
            {
                if (throwOnConflict)
                    throw new GitConflictException(std_out);
                return;
            }
            if (exitCode != 0)
            {
                throw new BashNonzeroExitCodeException(exitCode, std_err);
            }
        }
        /// <summary>This is more complicated than a simply alias in that it pulls the checked-out branch if there are new remote commits but no local unpushed commits. </summary>
        /// <param name="pull">If false, this behaves as the original `git checkout &lt;branchname&gt;`. </param>
        public static async Task Checkout(string gitDir, string branchName, bool @new = false, bool pull = true)
        {
            if (branchName == "-")
                pull = false;


            if (pull)
            {
                if (@new || branchName.StartsWith("origin/"))
                    pull = false;
                else
                {
                    var hasUnpushedCommitsTask = Task.FromResult(await HasUnpushedCommits(gitDir));
                    var hasUnpulledCommitsTask = Task.FromResult(true); // TODO

                    await Task.WhenAll(Fetch(gitDir), hasUnpushedCommitsTask, hasUnpulledCommitsTask);

                    pull = hasUnpulledCommitsTask.Result && !hasUnpushedCommitsTask.Result;
                }
            }
            if (branchName.StartsWith("origin/"))
            {
                await Fetch(gitDir);
            }
            string bash = $@"git checkout {(@new ? "-b" : "")} ""{branchName}""";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: gitDir);
            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, stdErr);

            //Console.WriteLine(stdOut);

            if (pull)
            {
                if (await GetCurrentRemoteBranch(gitDir) != null) // remove this check when hasUnpulledCommitsTask is implemented
                {
                    var currentHash = await GetCurrentHash(gitDir);

                    await Pull(gitDir);
                    if (currentHash != await GetCurrentHash(gitDir))
                    {
                        Console.WriteLine("Automatically pulled");
                    }
                    else
                    {
                        Console.WriteLine("Nothing to pull");
                    }
                }
            }
        }
        public static Task CreateBranch(string gitDir, string branchName, bool checkout = true)
        {
            if (!checkout) throw new NotImplementedException("bool checkout == false");

            return Checkout(gitDir, branchName, @new: true);
        }
        public static async Task<bool> HasUnpushedCommits(string gitDir)
        {
            string? remoteBranchName = await GetCurrentRemoteBranch(gitDir);
            if (remoteBranchName == null)
                return false;

            string bash = $"git log {remoteBranchName}..HEAD";
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);
            return std_out.Contains('\n');
        }
        public static async Task Fetch(string gitDir)
        {
            // --prune: delete local branches which are deleted on origin
            var (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git remote update --prune".Execute(cwd: gitDir);

            if (exitCode == 0 && stdOut.EndsWith("Fetching origin"))
                return;
            if (exitCode == 0 && stdOut == "")
                return; // the case where there's no upstream

            // TODO: if remote branches were deleted, check if they exist locally and point to the exact same commit, and delete them if so. 
            // if that's the current branch, then checkout the main branch first
            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, stdErr);
            throw NotImplementedException(exitCode, stdOut, stdErr, "git remote update");
        }
        public static async Task Pull(string gitDir)
        {
            var (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git pull --prune --rebase".Execute(cwd: gitDir);

            // TODO on pruned branches: see Fetch
            if (exitCode != 0)
                throw NotImplementedException(exitCode, stdOut, stdErr);
        }
        public static async Task<bool> IsGitRepo(string dir)
        {
            if (!Directory.Exists(dir))
                return false;
            string bash = ToBashOneliner($@"git -C ./ rev-parse; exit $?");

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: dir);
            return !std_err.StartsWith("fatal: not a git repository");
        }
        public static async Task New(string gitDir, string branchName, bool bringIndexOnly = false)
        {
            if (!IsValidBranchName(branchName))
                throw new ArgumentException($"'{branchName}' is not a valid branch name");

            var branchAlreadyExists = GetBranchExists(gitDir, branchName);
            Task<bool> isDirty = IsDirty(gitDir);
            var mainBranchName = GetMainBranchName(gitDir);
            var currentBranchName = GetCurrentBranch(gitDir);

            if (await branchAlreadyExists)
            {
                throw new BranchAlreadyExistsGitException();
            }

            string remoteMainBranchName = "origin/" + await mainBranchName;
            if (!await GetBranchExists(gitDir, remoteMainBranchName))
            {
                remoteMainBranchName = await mainBranchName;
            }
            if (await currentBranchName == await mainBranchName)
            {
                bool createdWipCommit = await StashIfNecessary();
                if (!createdWipCommit)
                {
                    // if we called `git new` on the master branch, we intended to move all commits up to origin/master to the new branch
                    await CreateBranch(gitDir, branchName, checkout: true);
                    await RepointBranch(gitDir, await mainBranchName, remoteMainBranchName, hard: true, assume_not_dirty: true);
                    Contract.Assert((await GetCurrentBranch(gitDir))?.Trim() == branchName);
                }
                else
                {
                    // but that doesn't make sense if --index-only was provided, so then we'll do something else
                    await Checkout(gitDir, remoteMainBranchName, pull: false);
                    await CreateBranch(gitDir, branchName, checkout: true);
                }


                await PopStashIfNecessary();

            }
            else
            {
                await StashIfNecessary();

                await Checkout(gitDir, remoteMainBranchName, pull: false);
                await CreateBranch(gitDir, branchName, checkout: true);

                await PopStashIfNecessary();
            }



            // returns whether a wip commit was created
            async Task<bool> StashIfNecessary()
            {
                if (await isDirty)
                {
                    await Stash(gitDir, bringIndexOnly);
                    if (bringIndexOnly)
                    {
                        return await Wip(gitDir);
                    }
                }
                return false;
            }
            async Task PopStashIfNecessary()
            {
                if (await isDirty)
                {
                    await PopStash(gitDir);
                }
            }
        }
        public static async Task TrackAllUntrackedFiles(string gitDir)
        {
            // This function doesn't really work, but it seems to work well with the Stash function
            IEnumerable<string> untrackedFiles = await GetUntrackedFiles(gitDir);

            // adds all untracked files to unstaged:
            string bash = "git add . && git reset -- " + untrackedFiles.Select(s => s.WrapInDoubleQuotes()).Join(" ");
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: gitDir);
            if (exitCode == 0 && (stdOut == "" || stdOut.StartsWith("Unstaged changes after reset:")))
                return;
            throw NotImplementedException(exitCode, stdOut, stdErr);
        }
        private static async Task RepointBranch(string gitDir, string branchName, string destRef, bool hard = false, bool assume_not_dirty = false)
        {
            // this is it's own method because later I might want to implement this without a trace in the history or e.g. what `git checkout -` would checkout to.
            // the current strategy doesn't do that; it's rather simple
            if (!assume_not_dirty)
                throw new NotImplementedException("assume_not_dirty == false");

            if (assume_not_dirty)
            {
                await Checkout(gitDir, branchName);
                await Reset(gitDir, destRef, hard: hard);
                await Checkout(gitDir, "-");
            }
        }

        public static async Task LoginToGitHub(string gitDir)
        {
            throw new NotImplementedException("LoginToGitHub");
        }
        public static async Task Automerge(string gitDir, string runId)
        {
            string bash = "gh run list";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: gitDir);

        }
        /// <param name="prId">Empty string for current branch.</param>
        public static async Task<string> GetPRBranchName(string gitDir, string prId = "")
        {
            string bash = $"gh pr view {prId} --json \"headRefName\"";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: gitDir);
            if (exitCode == 0)
            {
                var response = JsonSerializer.Deserialize<HeadRefNameResponse>(stdOut);
                if (response != null)
                    return response.headRefName;
            }

            throw NotImplementedException(exitCode, stdOut, stdErr);
        }

        class HeadRefNameResponse
        {
            public string headRefName { get; init; } = default!;
        }

        /// <param name="prId">Empty string for current branch.</param>
        public static async Task<string> GetPRBranchCommitHash(string gitDir, string prId = "")
        {
            string bash = $"gh pr view {prId} --json \"commits\" --jq '.[\"commits\"][-1][\"oid\"]'";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: gitDir);
            if (exitCode == 0)
            {
                if (Git.IsGitHash(stdOut))
                    return stdOut;
            }

            throw NotImplementedException(exitCode, stdOut, stdErr);
        }

        public static async Task Reset(string gitDir, string destRef, bool hard = false)
        {
            if (!hard)
                throw new NotImplementedException("hard == false");

            string bash = $"git reset --hard {destRef}";
            var (exitCode, stdOut, stdErr) = await bash.Execute(gitDir);

            if (exitCode == 0)
                return;
            throw NotImplementedException(exitCode, stdOut, stdErr);
        }

        /// <returns>The hash of the created commit; if any. </returns>
        public static async Task<bool> Wip(string gitDir)
        {
            if (!await IsDirty(gitDir))
            {
                return false;
            }

            string bash = ToBashOneliner(@"
			git add .;
			git commit -anm ""wip"";"
                .Dedent());

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: gitDir);

            if (exitCode != 0)
                throw NotImplementedException(exitCode, std_out, std_err);

            // std_out example: [init-jsdom 8f965bf] wip \n5 files changed, 27 insertions(+), 12 deletions(-)\ncreate mode 100644 test/index.ts
            if (!std_out.SubstringUntil("\n").Trim().EndsWith("wip"))
            {
                throw NotImplementedException(exitCode, std_out, std_err);
            }
            return true;
        }
        private static string ToBashOneliner(string s)
        {
            if (s.Contains("#"))
            {
                if (s.Split('\n').Any(line => line.Trim().StartsWith('#')))
                    throw new ArgumentException("No comments allowed");
            }

            return s.Replace("\r\n", " ").Replace("\n", " ");
        }

        public static async Task<IReadOnlyList<string>> GetStagedFiles(string gitDir)
        {
            var result = await "git diff --name-only --cached".Execute(cwd: gitDir);

            // output is a table, of which we only want the last column:
            // var result = output.StandardOutput.ToLines().Select(line => line.SubstringAfterLast("\t")).ToList();
            if (result.ExitCode != 0)
                throw new NotImplementedException();

            return result.StandardOutput
                         .ToLines()
                         .Where(line => !string.IsNullOrWhiteSpace(line))
                         .ToList();
        }
        public static async Task<IReadOnlyList<string>> GetUntrackedFiles(string gitDir)
        {
            var result = await "git ls-files --others --exclude-standard".Execute(cwd: gitDir);

            if (result.ExitCode != 0)
                throw new NotImplementedException();

            return result.StandardOutput
                         .ToLines()
                         .Where(line => !string.IsNullOrWhiteSpace(line))
                         .ToList();
        }


        public static NotImplementedException NotImplementedException(int exitCode, string stdOut, string stdErr, [CallerMemberName] string callerName = "")
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
            Contract.Assert(IsGitHash(s), message);
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
}
