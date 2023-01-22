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
    public class Git
    {
        public string SSH_SCRIPT { get; }
        public string Dir { get; }

        public Git(string directory, string? ssh_script = null)
        {
            this.Dir = directory;
            this.SSH_SCRIPT = ssh_script ?? "echo '' ";
        }


        public async Task<bool> IsDirty()
        {
            string bash = GitUtilities.ToBashOneliner(@"
if [ -z ""$(git status --porcelain)"" ]; then 
    echo false;
else 
    echo true;
fi");
            var debug = "git status --porcelain".Execute(cwd: this.Dir);
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);

            if (bool.TryParse(std_out, out bool result))
                return result;

            throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
        }
        public async Task<string> GetCurrentHash()
        {
            var (exitCode, std_out, std_err) = await "git rev-parse HEAD".Execute(cwd: this.Dir);
            return std_out;
        }
        public async Task<string?> GetCurrentBranch()
        {
            string bash = GitUtilities.ToBashOneliner(@"
CURRENT_BRANCH_NAME=$(git rev-parse --symbolic-full-name --abbrev-ref HEAD);
if [ ""$CURRENT_BRANCH_NAME"" = HEAD ] ; then
    echo '';
else
    echo $CURRENT_BRANCH_NAME;
fi");


            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);

            if (std_out.Length == 0) // we're detached
                return null;
            if (!std_out.Contains('\n'))
                return std_out;

            throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
        }
        public async Task<string?> GetCurrentRemoteBranch()
        {
            string bash = "git rev-parse --abbrev-ref --symbolic-full-name @{u}";

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);

            if (std_err.StartsWith("fatal: no upstream configured for branch '"))
                return null;
            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, std_err);
            if (std_out.Length == 0) // we're detached
                return null;
            if (!std_out.Contains('\n'))
                return std_out;
            throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
        }
        public async Task<bool> GetBranchExists(string branch)
        {
            if (branch.StartsWith("origin/"))
                await Fetch();

            string bash = $"git rev-parse --verify \"{branch}\"";
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);
            if (exitCode == 0 && std_out.Trim() != "")
                return true;
            if (std_err.StartsWith("fatal: Needed a single revision"))
                return false;
            else if (std_out.StartsWith("The system cannot find the path specified"))
            {
                Contract.Assert(Directory.Exists(this.Dir), $"The git directory '{this.Dir}' does not exist");
                Contract.Assert(false, $"The command '{bash}' failed in dir '{this.Dir}'");
            }
            throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
        }
        public async Task<string> GetDefaultBranchName()
        {
            var masterExists = await GetBranchExists("master");
            var mainExists = GetBranchExists("main");

            if (masterExists)
                return "master";
            if (await mainExists)
                return "main";
            throw new InvalidOperationException("No master nor main branch found");

        }
        
        public async Task<bool> Stash(bool indexOnly = false)
        {
            string? bash = null;
            if (indexOnly)
            {
                var stagedFiles = await GetStagedFiles();
                if (stagedFiles.Count != 0)
                {
                    // no need for --intent-to-add anymore because they're all staged anyway
                    bash = "git stash -- " + stagedFiles.Select(StringExtensions.WrapInSingleQuotes).Join(" ");
                }
            }
            if (bash == null)
            {
                await TrackAllUntrackedFiles();

                // we want that because untracked files may disappear in the stash. If you don't believe me:
                // - stash 1 tracked and 1 untracked file
                // - pop it while the tracked file results in a conflict (for sure modified vs delete conflict)
                // - notice that the untracked file is not popped
                // - it's still in the stash entry, which wasn't dropped
                // - if you, like me, drop the stash entry after resolving all conflicts, then you would be deleting work forever, because git decided not to pop the untracked files 🤷🏻‍
                bash = "git stash"; // -u is harmful here
            }


            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);
            if (std_err.EndsWith("You do not have the initial commit yet"))
                throw new NoInitialCommitGitException();
            if (std_out.StartsWith("Saved working directory"))
                return true;
            if (std_out.StartsWith("No local changes to save"))
                return false;

            throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
        }
        /// <param name="force"> If the files cannot be overwritten (because they are untracked) if true, it will be done anyway. </param>
        public async Task PopStash(bool force = false, bool throwOnConflict = false)
        {
            string bash = "git stash pop";

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);
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
                            string source = Path.Combine(this.Dir, filename);
                            string dest = Path.Combine(tempDir, filename);

                            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                            File.Move(source, dest, overwrite: true);
                        }
                        catch (IOException)
                        {
                            // nothing to do here if it fails. The next recursive call simply a StashFailedBecauseOfUntrackedFiles will be thrown.
                        }
                    }
                    await PopStash(force: false, throwOnConflict: throwOnConflict);
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
        public async Task Checkout(string branchName, bool @new = false, bool pull = true)
        {
            if (branchName == "-")
                pull = false;


            if (pull)
            {
                if (@new || branchName.StartsWith("origin/"))
                    pull = false;
                else
                {
                    var hasUnpushedCommitsTask = Task.FromResult(await HasUnpushedCommits());
                    var hasUnpulledCommitsTask = Task.FromResult(true); // TODO

                    await Task.WhenAll(Fetch(), hasUnpushedCommitsTask, hasUnpulledCommitsTask);

                    pull = hasUnpulledCommitsTask.Result && !hasUnpushedCommitsTask.Result;
                }
            }
            if (branchName.StartsWith("origin/"))
            {
                await Fetch();
            }
            string bash = $@"git checkout {(@new ? "-b" : "")} ""{branchName}""";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);
            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, stdErr);

            //Console.WriteLine(stdOut);

            if (pull)
            {
                if (await GetCurrentRemoteBranch() != null) // remove this check when hasUnpulledCommitsTask is implemented
                {
                    var currentHash = await GetCurrentHash();

                    await Pull();
                    if (currentHash != await GetCurrentHash())
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
        public Task CreateBranch(string branchName, bool checkout = true)
        {
            if (!checkout) throw new NotImplementedException("bool checkout == false");

            return Checkout(branchName);
        }
        public async Task<bool> HasUnpushedCommits()
        {
            string? remoteBranchName = await GetCurrentRemoteBranch();
            if (remoteBranchName == null)
                return false;

            string bash = $"git log {remoteBranchName}..HEAD";
            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);
            return std_out.Contains('\n');
        }
        public async Task Fetch()
        {
            // --prune: delete local branches which are deleted on origin
            var (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git remote update --prune".Execute(cwd: this.Dir);

            if (exitCode == 0 && stdOut.EndsWith("Fetching origin"))
                return;
            if (exitCode == 0 && stdOut == "")
                return; // the case where there's no upstream

            if (exitCode != 0)
                throw new BashNonzeroExitCodeException(exitCode, stdErr);
            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr, "git remote update");
        }
        public async Task Pull()
        {
            var (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git pull --prune --rebase".Execute(cwd: this.Dir);

            // TODO on pruned branches: see Fetch
            if (exitCode != 0)
                throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }
        public async Task<bool> IsGitRepo()
        {
            if (!Directory.Exists(this.Dir))
                return false;
            string bash = GitUtilities.ToBashOneliner($@"git -C ./ rev-parse; exit $?");

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);
            return !std_err.StartsWith("fatal: not a git repository");
        }
        public async Task New(string branchName, bool bringIndexOnly = false)
        {
            if (!GitUtilities.IsValidBranchName(branchName))
                throw new ArgumentException($"'{branchName}' is not a valid branch name");

            var branchAlreadyExists = GetBranchExists(branchName);
            Task<bool> isDirty = IsDirty();
            var mainBranchName = GetDefaultBranchName();
            var currentBranchName = GetCurrentBranch();

            if (await branchAlreadyExists)
            {
                throw new BranchAlreadyExistsGitException();
            }

            string remoteMainBranchName = "origin/" + await mainBranchName;
            if (!await GetBranchExists(remoteMainBranchName))
            {
                remoteMainBranchName = await mainBranchName;
            }
            if (await currentBranchName == await mainBranchName)
            {
                bool createdWipCommit = await StashIfNecessary();
                if (!createdWipCommit)
                {
                    // if we called `git new` on the master branch, we intended to move all commits up to origin/master to the new branch
                    await CreateBranch(branchName, checkout: true);
                    await RepointBranch(await mainBranchName, remoteMainBranchName, hard: true, assume_not_dirty: true);
                    Contract.Assert((await GetCurrentBranch())?.Trim() == branchName);
                }
                else
                {
                    // but that doesn't make sense if --index-only was provided, so then we'll do something else
                    await Checkout(remoteMainBranchName, pull: false);
                    await CreateBranch(branchName, checkout: true);
                }


                await PopStashIfNecessary();

            }
            else
            {
                await StashIfNecessary();

                await Checkout(remoteMainBranchName, pull: false);
                await CreateBranch(branchName, checkout: true);

                await PopStashIfNecessary();
            }



            // returns whether a wip commit was created
            async Task<bool> StashIfNecessary()
            {
                if (await isDirty)
                {
                    await Stash(bringIndexOnly);
                    if (bringIndexOnly)
                    {
                        return await Wip();
                    }
                }
                return false;
            }
            async Task PopStashIfNecessary()
            {
                if (await isDirty)
                {
                    await PopStash();
                }
            }
        }
        public async Task TrackAllUntrackedFiles()
        {
            // This function doesn't really work, but it seems to work well with the Stash function
            IEnumerable<string> untrackedFiles = await GetUntrackedFiles();

            // adds all untracked files to unstaged:
            string bash = "git add . && git reset -- " + untrackedFiles.Select(s => s.WrapInDoubleQuotes()).Join(" ");
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);
            if (exitCode == 0 && (stdOut == "" || stdOut.StartsWith("Unstaged changes after reset:")))
                return;
            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }
        private async Task RepointBranch(string branchName, string destRef, bool hard = false, bool assume_not_dirty = false)
        {
            // this is it's own method because later I might want to implement this without a trace in the history or e.g. what `git checkout -` would checkout to.
            // the current strategy doesn't do that; it's rather simple
            if (!assume_not_dirty)
                throw new NotImplementedException("assume_not_dirty == false");

            if (assume_not_dirty)
            {
                await Checkout(branchName);
                await Reset(destRef, hard: hard);
                await Checkout("-");
            }
        }

        //public static async Task LoginToGitHub()
        //{
        //    throw new NotImplementedException("LoginToGitHub");
        //}
        public async Task Automerge(string runId)
        {
            string bash = "gh run list";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);

        }
        /// <param name="prId">Empty string for current branch.</param>
        public async Task<string> GetPRBranchName(string prId = "")
        {
            string bash = $"gh pr view \"{prId}\" --json \"headRefName\"";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);
            if (exitCode == 0)
            {
                var response = JsonSerializer.Deserialize<HeadRefNameResponse>(stdOut);
                if (response != null)
                    return response.headRefName;
            }

            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }

        class HeadRefNameResponse
        {
            public string headRefName { get; init; } = default!;
        }

        /// <param name="prId">Empty string for current branch.</param>
        public async Task<string> GetPRBranchCommitHash(string prId = "")
        {
            string bash = $"gh pr view \"{prId}\" --json \"commits\" --jq '.[\"commits\"][-1][\"oid\"]'";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);
            if (exitCode == 0)
            {
                if (GitUtilities.IsGitHash(stdOut))
                    return stdOut;
            }

            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }
        // <param name="prId">Empty string for current branch.</param>
        public async Task<string> GetPRBaseBranch(string prId = "")
        {
            string bash = $"gh pr view \"{prId}\" --json \"baseRefName\"";
            var (exitCode, stdOut, stdErr) = await bash.Execute(cwd: this.Dir);
            if (exitCode == 0)
            {
                var response = JsonSerializer.Deserialize<BaseRefNameResponse>(stdOut);
                if (response != null)
                {
                    var result = response.baseRefName;
                    if (GitUtilities.IsValidBranchName(result))
                        return "origin/" + result;
                    else if (GitUtilities.IsGitHash(result))
                        return result;
                }
            }

            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }
        class BaseRefNameResponse
        {
            public string baseRefName { get; init; } = default!;
        }
        public async Task Reset(string destRef, bool hard = false)
        {
            if (!hard)
                throw new NotImplementedException("hard == false");

            string bash = $"git reset --hard {destRef}";
            var (exitCode, stdOut, stdErr) = await bash.Execute(this.Dir);

            if (exitCode == 0)
                return;
            throw GitUtilities.NotImplementedException(exitCode, stdOut, stdErr);
        }

        /// <returns>The hash of the created commit; if any. </returns>
        public async Task<bool> Wip()
        {
            if (!await IsDirty())
            {
                return false;
            }

            string bash = GitUtilities.ToBashOneliner(@"
			git add .;
			git commit -anm ""wip"";"
                .Dedent());

            var (exitCode, std_out, std_err) = await bash.Execute(cwd: this.Dir);

            if (exitCode != 0)
                throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);

            // std_out example: [init-jsdom 8f965bf] wip \n5 files changed, 27 insertions(+), 12 deletions(-)\ncreate mode 100644 test/index.ts
            if (!std_out.SubstringUntil("\n").Trim().EndsWith("wip"))
            {
                throw GitUtilities.NotImplementedException(exitCode, std_out, std_err);
            }
            return true;
        }

        public async Task<IReadOnlyList<string>> GetStagedFiles()
        {
            var result = await "git diff --name-only --cached".Execute(cwd: this.Dir);

            // output is a table, of which we only want the last column:
            // var result = output.StandardOutput.ToLines().Select(line => line.SubstringAfterLast("\t")).ToList();
            if (result.ExitCode != 0)
                throw new NotImplementedException();

            return result.StandardOutput
                         .ToLines()
                         .Where(line => !string.IsNullOrWhiteSpace(line))
                         .ToList();
        }
        public async Task<IReadOnlyList<string>> GetUntrackedFiles()
        {
            var result = await "git ls-files --others --exclude-standard".Execute(cwd: this.Dir);

            if (result.ExitCode != 0)
                throw new NotImplementedException();

            return result.StandardOutput
                         .ToLines()
                         .Where(line => !string.IsNullOrWhiteSpace(line))
                         .ToList();
        }



    }
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
            Contract.Assert(IsGitHash(s), message);
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
}
