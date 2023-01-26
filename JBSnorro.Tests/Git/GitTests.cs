#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JBSnorro.Tests;
using JBSnorro.IO;

namespace JBSnorro.Csx.Tests
{
    public class GitTestsBase
    {
        protected const string ROOT_HASH = "818c7ad1722e9c4fe682b30ade4413bf1e36c542";

        // if SSH_FILE cannot be found:
        // - in testing, add JBSnorro.Tests/Properties/.runSettings as VS -> Test -> Configure Run Settings -> Select ...
        // - in debugging, add the path to the runSettings as debug env var RUNSETTINGS_PATH
        protected static string ssh_file => EnvironmentExtensions.GetRequiredEnvironmentVariable("SSH_FILE");
        protected static string ssh_key_path => Path.GetFullPath(ssh_file.ExpandTildeAsHomeDir()).ToBashPath(false);
        protected static string init_ssh_agent_path = TestProject.CurrentDirectory.ToBashPath(false) + "/init-ssh-agent.sh";
        private static string GIT_SSH_COMMAND => $"GIT_SSH_COMMAND=\"ssh -i {ssh_key_path} -F /dev/null\"";
        protected static string SSH_SCRIPT => $"source {init_ssh_agent_path} && ssh-add {ssh_key_path} && export {GIT_SSH_COMMAND}";

        private IAsyncDisposable? cleanup;
        [TestCleanup]
        public async Task Cleanup()
        {
            if (cleanup != null)
            {
                await cleanup.DisposeAsync();
            }
        }
        protected async Task<IGitRepo> InitEmptyRepo(Func<string /*dir*/, IRemoteGitRepo>? remoteFactory = null)
        {
            string dir = IOExtensions.CreateTemporaryDirectory();
            this.cleanup = TempFileCleanup.Register(dir + "/");
            var result = await "git init; git config user.name 'tester'; git config user.email 'tester@test.com'".Execute(cwd: dir);

            Assert.AreEqual(result.ExitCode, 0, result.ErrorOutput);
            Assert.IsTrue(result.StandardOutput.StartsWith("Initialized empty Git repository"));

            var remote = (remoteFactory ?? DefaultRemoteIGitRepoFactory)(dir);
            return IGitRepo.Create(dir, remote);

            static IRemoteGitRepo DefaultRemoteIGitRepoFactory(string dir)
            {
                return new RemoteRepoWithNoUpdates();
            }
        }
        protected async Task<IGitRepo> InitRepo(Func<string /*dir*/, IRemoteGitRepo>? remoteFactory = null)
        {
            var repo = await InitEmptyRepo(remoteFactory);
            var result = await "git commit --allow-empty -m 'First commit'".Execute(cwd: repo.Dir);
            Assert.IsTrue(result.StandardOutput.EndsWith("First commit"));
            return repo;
        }
        protected async Task<IGitRepo> InitRepoWithUntrackedFile()
        {
            var repo = await InitRepo();
            using (File.Create(Path.Combine(repo.Dir, "tmp"))) { }

            return repo;
        }
        /// <summary> Tracked means not untracked, but not staged either.  </summary>
        protected async Task<IGitRepo> InitRepoWithTrackedFile()
        {
            var repo = await InitRepoWithStagedFile();
            var result = await "git reset -- tmp".Execute(cwd: repo.Dir);
            Assert.AreEqual(result.ExitCode, 0);

            return repo;
        }
        protected async Task<IGitRepo> InitRepoWithStagedFile()
        {
            var repo = await InitRepoWithUntrackedFile();
            var result = await "git add tmp".Execute(cwd: repo.Dir);
            Assert.AreEqual(result.ExitCode, 0);

            return repo;
        }
        protected async Task<IGitRepo> InitRepoWithTrackedUntrackedAndStagedFiles(string? newBranchName = "new_branch")
        {
            var repo = await InitRepo();

            if (newBranchName is not null)
                await repo.Checkout(newBranchName, @new: true);

            File.WriteAllText(Path.Combine(repo.Dir, "a"), "contents"); // a for added
            File.WriteAllText(Path.Combine(repo.Dir, "m"), "contents"); // m for modified
            File.WriteAllText(Path.Combine(repo.Dir, "u"), "contents"); // u for untracked

            var result = await @"
            git add m;
            git commit -m 'add m';
            git add a;
            echo 'contents_of_m' >> m;   # modify t
            ".Dedent().Execute(cwd: repo.Dir);

            Assert.AreEqual(result.ExitCode, 0);
            return repo;
        }
        protected async Task<IGitRepo> InitRepoWithStash()
        {
            var repo = await InitRepoWithUntrackedFile();
            var result = await "git stash -u".Execute(cwd: repo.Dir);
            Assert.AreEqual(result.ExitCode, 0);

            return repo;
        }
        protected async Task<IGitRepo> InitDetachedState()
        {
            var repo = await InitRepo();
            var result = await "git commit --allow-empty -m 'Second commit'; git checkout HEAD~".Execute(cwd: repo.Dir);
            Assert.IsTrue(result.StandardOutput.EndsWith("Second commit"));

            return repo;
        }
        protected async Task<IGitRepo> InitRepoWithCommit()
        {
            var repo = await InitRepoWithStagedFile();
            await "git commit -m 'contains file'".Execute(cwd: repo.Dir);

            return repo;
        }
        protected async Task<IGitRepo> InitRemoteRepo()
        {
            var repo = await InitRepo(dir => IRemoteGitRepo.Create(dir, SSH_SCRIPT));

            var sshKey = File.ReadAllLines(ssh_file.ToWindowsPath());
            Assert.AreEqual(27, sshKey.Length, delta: 1);

            // var (exitCode, stdOut, stdErr) = await $"source ../startup.sh".Execute(cwd: git.Dir);
            var (exitCode, stdOut, stdErr) = await SSH_SCRIPT.Execute(cwd: repo.Dir);
            // (exitCode, stdOut, stdErr) = await $"source ../startup.sh  && echo '{pass}' | SSH_ASKPASS=./ap.sh ssh-add".Execute(cwd: git.Dir);
            if (stdErr.StartsWith("@@@@@@"))
            {
                await $"sudo chmod 600 {ssh_file}".Execute(cwd: repo.Dir); // or just execute manually if I ever reinstalled Windows or something
                (exitCode, stdOut, stdErr) = await $"source ../startup.sh && ssh-add {ssh_file}".Execute(cwd: repo.Dir);
            }
            (exitCode, stdOut, stdErr) = await "git remote add origin git@github.com:JeroenBos/TestPlayground.git".Execute(cwd: repo.Dir);
            Assert.AreEqual((exitCode, stdOut, stdErr), (0, "", ""));

            try
            {
                (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git fetch".Execute(cwd: repo.Dir, cancellationToken: new CancellationTokenSource(10_000).Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Some ssh-agent config is probably interfering with the ssh being used under test.");
                Console.WriteLine("Most likely the script is waiting for the ssh passphrase");
                throw;
            }
            Assert.AreEqual((exitCode, stdOut), (0, ""), message: stdErr);
            //Console.WriteLine(stdOut);
            //Assert.AreEqual(stdErr.Split('\n').Length, 3, message: stdOut);
            var stdErrLines = stdErr.Split('\n')
                                    .Where(line => !line.StartsWith("warning", StringComparison.OrdinalIgnoreCase)) // "warning: no common commits", and "Warning: Permanently added the RSA host ..."
                                    .ToArray();
            Assert.IsTrue(stdErrLines[0].StartsWith("Identity added"));
            Assert.IsTrue(stdErrLines[1].StartsWith("From github.com:JeroenBos/TestPlayground"), stdErrLines[1]);
            Assert.IsTrue(stdErrLines[2].StartsWith(" * [new branch]      master     -> origin/master"), stdErrLines[2]);

            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git branch --set-upstream-to=origin/master master".Execute(cwd: repo.Dir);
            Assert.AreEqual(exitCode, 0);
            // the following depends on git version or something:
            Assert.IsTrue(stdOut.IsAnyOf("Branch 'master' set up to track remote branch 'master' from 'origin'.",
                                         "branch 'master' set up to track 'origin/master'."), stdOut);
            Assert.AreEqual(stdErr.Split('\n').Length, 1);
            Assert.IsTrue(stdErr.StartsWith("Identity added"));

            (exitCode, stdOut, stdErr) = await $"git reset --hard {ROOT_HASH}".Execute(cwd: repo.Dir);
            Assert.AreEqual((exitCode, stdErr), (0, ""));
            Assert.AreEqual(stdOut.Split('\n').Length, 1);
            Assert.IsTrue(stdOut.StartsWith("HEAD is now at"));

            // this can throw with error containing "! [remote rejected] master -> master (cannot lock ref 'refs/heads/master'"
            // it's presumably due to parallelism, but that shouldn't be there :/
            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git fetch && git push --force-with-lease".Execute(cwd: repo.Dir);
            Assert.AreEqual((exitCode, stdOut), (0, ""), message: stdErr);
            // Assert.AreEqual(stdErr.Split('\n').Length, 2); // 3 with when (forced-updated)
            // Assert.IsTrue(stdErr.Split('\n')[1].StartsWith("Everything up-to-date"));
            return repo;
        }
        protected async Task<IGitRepo> InitRemoteRepoWithCommit(Reference<string>? commitHash = null)
        {
            var repo = await InitRemoteRepo();
            using (File.Create(Path.Combine(repo.Dir, "tmp"))) { }

            var (exitCode, stdOut, stdErr) = await "git add . && git commit -m 'first pushed file'".Execute(cwd: repo.Dir);
            Assert.AreEqual((exitCode, stdErr), (0, ""));
            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git push".Execute(cwd: repo.Dir);
            Assert.AreEqual(exitCode, 0);
            Assert.AreEqual(stdOut, "");

            if (commitHash != null)
            {
                commitHash.Value = await repo.GetCurrentHash();
            }
            return repo;
        }
        protected async Task<IGitRepo> InitRemoteRepoWithRemoteCommit(Reference<string>? remoteCommitHash = null)
        {
            var repo = await InitRemoteRepoWithCommit(remoteCommitHash);

            // remove commit locally
            await "git reset --hard @~".Execute(cwd: repo.Dir);

            return repo;
        }
    }

    [TestClass]
    public class GitBasicTests
    {
        [TestMethod]
        public async Task CheckGitBashInstallation()
        {
            string dir = IOExtensions.CreateTemporaryDirectory();
            var result = await "echo hi".Execute(cwd: dir);

            Assert.AreEqual(expected: 0, result.ExitCode);

        }
    }
    [TestClass]
    public class IsDirtyTests : GitTestsBase
    {
        [TestMethod]
        public async Task TestEmptyRepoIsNotIsDirty()
        {
            var repo = await InitEmptyRepo();

            bool dirty = await repo.IsDirty();

            Assert.IsFalse(dirty);
        }
        [TestMethod]
        public async Task TestEmptyWithUntrackedFileIsDirty()
        {
            var repo = await InitRepoWithUntrackedFile();

            bool dirty = await repo.IsDirty();

            Assert.IsTrue(dirty);
        }

    }
    [TestClass]
    public class GetCurrentBranchTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Get_Current_Branch_On_Empty_Repo()
        {
            // empty repo is the weird state before any commit has ever happened
            var repo = await InitEmptyRepo();

            string? branchName = await repo.GetCurrentBranch();

            Assert.IsTrue(branchName is null);
        }
        [TestMethod]
        public async Task Test_Get_Current_Branch()
        {
            var repo = await InitRepo();

            string? branchName = await repo.GetCurrentBranch();

            Assert.IsTrue(branchName == "master" || branchName == "main");
        }
        [TestMethod]
        public async Task Test_Current_Branch_Is_None_When_In_Detached_State()
        {
            var repo = await InitDetachedState();

            string? branchName = await repo.GetCurrentBranch();

            Assert.IsNull(branchName);
        }
    }
    [TestClass]
    public class GetBranchExistsTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Get_Non_Existing_Branch_Does_Not_Exit()
        {
            var repo = await InitRepo();

            bool exists = await repo.GetBranchExists("doesntexist");

            Assert.IsFalse(exists);
        }
        [TestMethod]
        public async Task Test_Current_Branch_Exists()
        {
            var repo = await InitRepo();
            string currentBranchName = (await repo.GetCurrentBranch())!;

            bool exists = await repo.GetBranchExists(currentBranchName);

            Assert.IsTrue(exists);
        }
    }
    [TestClass]
    public class StashTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Stash_Without_Anything_To_Stash()
        {
            var repo = await InitRepo();

            bool stashed = await repo.Stash();

            Assert.IsFalse(stashed);
        }
        // TODO: [TestMethod]
        public async Task Test_Stash_With_Untracked_File_Stashes()
        {
            var repo = await InitRepoWithUntrackedFile();

            bool stashed = await repo.Stash();

            Assert.IsTrue(stashed);
            Assert.IsFalse(await repo.IsDirty());
        }

        // TODO: [TestMethod]
        public async Task Test_Stash_With_Tracked_File_Stashes()
        {
            var repo = await InitRepoWithStagedFile();

            bool stashed = await repo.Stash();

            Assert.IsTrue(stashed);
            Assert.IsFalse(await repo.IsDirty());
        }
    }
    [TestClass]
    public class PopStashTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Pop_Stash_Without_Stash_Throws()
        {
            var repo = await InitRepo();

            await Assert.ThrowsExceptionAsync<StashEmptyGitException>(async () => await repo.PopStash());
        }
        [TestMethod]
        public async Task Test_Pop_Stash_Pops()
        {
            var repo = await InitRepoWithStash();

            await repo.PopStash();

            Assert.IsTrue(await repo.IsDirty());
        }
    }
    [TestClass]
    public class IsIGitRepoTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Git_Repo_Repository_Is_Repo()
        {
            var repo = await InitRepo();

            bool isGitRepo = await repo.IsGitRepo();

            Assert.IsTrue(isGitRepo);
        }
        [TestMethod]
        public async Task Test_NonGit_Repo_Repository_Is_Not_A_Repo()
        {
            string dir = IOExtensions.CreateTemporaryDirectory();

            bool isGitRepo = await IGitRepo.Create(dir).IsGitRepo();

            Assert.IsFalse(isGitRepo);
        }
    }
    [TestClass]
    public class GetUntrackedFilesTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Untracked_File_Is_Listed()
        {
            var repo = await InitRepoWithUntrackedFile();

            var untrackedFiles = await repo.GetUntrackedFiles();

            Assert.AreEqual(1, untrackedFiles.Count);
            Assert.AreEqual("tmp", untrackedFiles[0]);
        }
        [TestMethod]
        public async Task Test_Modified_Files_Is_Not_Listed()
        {
            var repo = await InitRepoWithStagedFile();

            var untrackedFiles = await repo.GetUntrackedFiles();

            Assert.AreEqual(0, untrackedFiles.Count);
        }
    }
    [TestClass]
    public class TrackUntrackedFilesTests : GitTestsBase
    {
        // these tests fail, but they're less important than the git stash tests, which is why I created this.
        // TODO: [TestMethod]
        public async Task Test_Track_Untracked_File()
        {
            var repo = await InitRepoWithUntrackedFile();

            // Act
            await repo.TrackAllUntrackedFiles();

            // Assert
            var untrackedFiles = await repo.GetUntrackedFiles();
            var stagedFiles = await repo.GetStagedFiles();

            Assert.AreEqual(0, untrackedFiles.Count);
            Assert.AreEqual(0, stagedFiles.Count);
            // I don't have a method GetUnstagedFiles yet: Assert.AreEqual(1, unstagedFiles.Count);
        }
        // TODO: [TestMethod]
        public async Task Test_Tracking_Tracked_File_Is_Noop()
        {
            var repo = await InitRepoWithTrackedFile();

            // Act
            await repo.TrackAllUntrackedFiles();

            // Assert
            var untrackedFiles = await repo.GetUntrackedFiles();
            var stagedFiles = await repo.GetStagedFiles();

            Assert.AreEqual(0, untrackedFiles.Count);
            Assert.AreEqual(0, stagedFiles.Count);
            // I don't have a method GetUnstagedFiles yet: Assert.AreEqual(1, unstagedFiles.Count);
        }
    }

    public class GitHubTestsBase : GitTestsBase
    {
        protected async Task<IGitHubRepo> InitGitHubRepoWithRemoteCommit(Reference<string>? remoteCommitHash = null)
        {
            IGitRepo git = await base.InitRemoteRepoWithRemoteCommit(remoteCommitHash);
            return IGitHubRepo.Create(git);
        }
    }
    [TestClass]
    public class NewTests : GitHubTestsBase
    {
        [TestMethod]
        public async Task Test_New_Takes_Untracked_tracked_and_staged()
        {
            var repo = await InitRepoWithTrackedUntrackedAndStagedFiles();

            await repo.New("a", bringIndexOnly: false);

            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "a")));
            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "m")));
            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "u")));
        }

        [TestMethod]
        public async Task Test_New_Only_Takes_Staged_With_Option_Index()
        {
            // but in CI this test succeed :S :S
            // that means it's flaky tests. They depend on order of execution. Probably the SSH_SCRIPT bug
            var repo = await InitRepoWithTrackedUntrackedAndStagedFiles("first_branch");

            await repo.New("newBranch", bringIndexOnly: true);

            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "a")));
            Assert.IsFalse(File.Exists(Path.Combine(repo.Dir, "m")));
            Assert.IsFalse(File.Exists(Path.Combine(repo.Dir, "u")));

            // destroy state to inspect other branch state
            var (exitCode, std, err) = await @"git add . && git reset --hard && git checkout first_branch".Execute(cwd: repo.Dir);
            Assert.AreEqual(exitCode, 0);


            Assert.IsFalse(File.Exists(Path.Combine(repo.Dir, "a")));
            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "m")));
            Assert.IsTrue(File.Exists(Path.Combine(repo.Dir, "u")));
        }

        [TestMethod]
        public async Task Test_New_Pulls_Remote()
        {
            var commitHash = new Reference<string>();
            var repo = await InitGitHubRepoWithRemoteCommit(commitHash);
            await "git checkout -b somebranch".Execute(cwd: repo.Dir);
            Assert.AreEqual(await repo.GetCurrentHash(), ROOT_HASH);

            await repo.New("newbranch");

            Assert.AreEqual(await repo.GetCurrentHash(), commitHash.Value);
        }
    }
    [TestClass]
    public class TestGHGetPrName : GitHubTestsBase
    {
        // [TestMethod] // reimplement when GH login works from CI
        public async Task Test_Get_Pr_Name()
        {
            var repo = await InitGitHubRepoWithRemoteCommit();

            var branchName = await repo.GetPRBranchName("1");

            Assert.AreEqual("patch-1", branchName);
        }
    }
    [TestClass]
    public class TestGHGetPrCommitHash : GitHubTestsBase
    {
        // [TestMethod] // reimplement when GH login works from CI
        public async Task Test_Get_Pr_CommitHash()
        {
            var repo = await InitGitHubRepoWithRemoteCommit();

            var branchName = await repo.GetPRBranchCommitHash("1");

            Assert.AreEqual("0b439655789e463e598535fb619a43b8bb1af8e1", branchName);
        }
    }
    [TestClass]
    public class TestGHGetPrBaseName : GitHubTestsBase
    {
        // [TestMethod] // reimplement when GH login works from CI
        public async Task Test_Get_Pr_BaseName()
        {
            var repo = await InitGitHubRepoWithRemoteCommit();

            var branchName = await repo.GetPRBaseBranch("1");

            Assert.AreEqual("origin/master", branchName);
        }
    }
    class RemoteRepoWithNoUpdates : IRemoteGitRepo
    {
        public string SSH_SCRIPT => ":;";  // :; is a no-op

        public Task Fetch() => Task.CompletedTask;
        public Task Pull() => Task.CompletedTask;
    }
}
