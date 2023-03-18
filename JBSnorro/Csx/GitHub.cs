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
using System.Diagnostics;
using JBSnorro.Csx;

namespace JBSnorro.Csx;

public interface IGitHubAdapter
{
    public static IGitHubRepo Create(IGitRepo gitRepo) => new GitHubAdapterAndGitRepoComposition(gitRepo);


    /// <param name="prId">Empty string for current branch.</param>
    Task<string> GetPRBranchName(string prId);
    /// <param name="prId">Empty string for current branch.</param>
    Task<string> GetPRBranchCommitHash(string prId);
    /// <param name="prId">Empty string for current branch.</param>
    Task<string> GetPRBaseBranch(string prId);
}


internal interface GitHubAdapterMixin : IGitHubAdapter
{
    internal string Dir { get; }
    async Task<string> IGitHubAdapter.GetPRBranchName(string prId)
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
    async Task<string> IGitHubAdapter.GetPRBranchCommitHash(string prId)
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
    async Task<string> IGitHubAdapter.GetPRBaseBranch(string prId)
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

}


public interface IGitHubRepo : IGitHubAdapter, IGitRepo, IRemoteGitRepo
{
}

/// <summary>
/// This type merely composed <see cref="IGitHubAdapter"/> and <see cref="IGitRepo"/> (which contains a <see cref="IRemoteGitRepo"/>).
/// </summary>
class GitHubAdapterAndGitRepoComposition : GitHubAdapterMixin, IGitHubRepo
{
    public IGitRepo GitRepo { get; }

    [DebuggerHidden] public GitHubAdapterAndGitRepoComposition(IGitRepo gitRepo) => this.GitRepo = gitRepo ?? throw new ArgumentNullException(nameof(gitRepo));

    [DebuggerHidden] IRemoteGitRepo IGitRepo.Remote => this.GitRepo.Remote;
    [DebuggerHidden] string GitHubAdapterMixin.Dir => this.GitRepo.Dir;
    [DebuggerHidden] string IGitRepo.Dir => this.GitRepo.Dir;
    [DebuggerHidden] Task<bool> IGitRepo.IsDirty() => this.GitRepo.IsDirty();
    [DebuggerHidden] Task<string> IGitRepo.GetCurrentHash() => this.GitRepo.GetCurrentHash();
    [DebuggerHidden] Task<string?> IGitRepo.GetCurrentBranch() => this.GitRepo.GetCurrentBranch();
    [DebuggerHidden] Task<string?> IGitRepo.GetCurrentRemoteBranch() => this.GitRepo.GetCurrentRemoteBranch();
    [DebuggerHidden] Task<bool> IGitRepo.GetBranchExists(string branch) => this.GitRepo.GetBranchExists(branch);
    [DebuggerHidden] Task<string> IGitRepo.GetDefaultBranchName() => this.GitRepo.GetDefaultBranchName();
    [DebuggerHidden] Task<bool> IGitRepo.Stash(bool indexOnly) => this.GitRepo.Stash(indexOnly);
    [DebuggerHidden] Task IGitRepo.PopStash(bool force, bool throwOnConflict) => this.GitRepo.PopStash(force, throwOnConflict);
    [DebuggerHidden] Task IGitRepo.Checkout(string branchName, bool @new, bool pull) => this.GitRepo.Checkout(branchName, @new, pull);
    [DebuggerHidden] Task IGitRepo.CreateBranch(string branchName, bool checkout) => this.GitRepo.CreateBranch(branchName, checkout);
    [DebuggerHidden] Task<bool> IGitRepo.HasUnpushedCommits() => this.GitRepo.HasUnpushedCommits();
    [DebuggerHidden] Task<bool> IGitRepo.IsGitRepo() => this.GitRepo.IsGitRepo();
    [DebuggerHidden] Task IGitRepo.New(string branchName, bool bringIndexOnly) => this.GitRepo.New(branchName, bringIndexOnly);
    [DebuggerHidden] Task IGitRepo.TrackAllUntrackedFiles() => this.GitRepo.TrackAllUntrackedFiles();
    [DebuggerHidden] Task<bool> IGitRepo.Wip() => this.GitRepo.Wip();
    [DebuggerHidden] Task<IReadOnlyList<string>> IGitRepo.GetStagedFiles() => this.GitRepo.GetStagedFiles();
    [DebuggerHidden] Task<IReadOnlyList<string>> IGitRepo.GetUntrackedFiles() => this.GitRepo.GetUntrackedFiles();
}





