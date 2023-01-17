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

namespace JBSnorro.Csx.Tests
{
    public class GitTestsBase
    {
        protected const string ROOT_HASH = "818c7ad1722e9c4fe682b30ade4413bf1e36c542";

        // if SSH_FILE cannot be found, consider adding JBSnorro.Tests/Properties/.runSettings as VS -> Test -> Configure Run Settings -> Select ...
        protected static string ssh_file => Environment.GetEnvironmentVariable("SSH_FILE") ?? throw new Exception("Env var 'SSH_FILE' not found");
        protected static string ssh_key_path => Path.GetFullPath(ssh_file.ExpandTildeAsHomeDir()).ToBashPath(false);
        protected static string init_ssh_agent_path = TestProject.CurrentDirectory.ToBashPath(false) + "/init-ssh-agent.sh";
        private static string GIT_SSH_COMMAND => $"GIT_SSH_COMMAND=\"ssh -i {ssh_key_path} -F /dev/null\"";
        protected static string SSH_SCRIPT => $"source {init_ssh_agent_path} && ssh-add {ssh_key_path} && export {GIT_SSH_COMMAND}";

        protected static async Task<string> InitEmptyRepo()
        {
            string dir = IOExtensions.CreateTemporaryDirectory();
            var result = await "git init; git config user.name 'tester'; git config user.email 'tester@test.com'".Execute(cwd: dir);

            Assert.AreEqual(result.ExitCode, 0, result.ErrorOutput);
            Assert.IsTrue(result.StandardOutput.StartsWith("Initialized empty Git repository"));
            return dir;
        }
        protected static async Task<string> InitRepo()
        {
            string dir = await InitEmptyRepo();
            var result = await "git commit --allow-empty -m 'First commit'".Execute(cwd: dir);
            Assert.IsTrue(result.StandardOutput.EndsWith("First commit"));
            return dir;
        }
        protected static async Task<string> InitRepoWithUntrackedFile()
        {
            string dir = await InitRepo();
            using (File.Create(Path.Combine(dir, "tmp"))) { }

            return dir;
        }
        /// <summary> Tracked means not untracked, but not staged either.  </summary>
        protected static async Task<string> InitRepoWithTrackedFile()
        {
            string dir = await InitRepoWithStagedFile();
            var result = await "git reset -- tmp".Execute(cwd: dir);
            Assert.AreEqual(result.ExitCode, 0);

            return dir;
        }
        protected static async Task<string> InitRepoWithStagedFile()
        {
            string dir = await InitRepoWithUntrackedFile();
            var result = await "git add tmp".Execute(cwd: dir);
            Assert.AreEqual(result.ExitCode, 0);

            return dir;
        }
        protected static async Task<string> InitRepoWithTrackedUntrackedAndStagedFiles(string? newBranchName = "new_branch")
        {
            string dir = await InitRepo();

            if (newBranchName is not null)
                await Git.Checkout(dir, newBranchName, @new: true);

            File.WriteAllText(Path.Combine(dir, "a"), "contents"); // a for added
            File.WriteAllText(Path.Combine(dir, "m"), "contents"); // m for modified
            File.WriteAllText(Path.Combine(dir, "u"), "contents"); // u for untracked

            var result = await @"
            git add m;
            git commit -m 'add m';
            git add a;
            echo 'contents_of_m' >> m;   # modify t
            ".Dedent().Execute(cwd: dir);

            Assert.AreEqual(result.ExitCode, 0);
            return dir;
        }
        protected static async Task<string> InitRepoWithStash()
        {
            string dir = await InitRepoWithUntrackedFile();
            var result = await "git stash -u".Execute(cwd: dir);
            Assert.AreEqual(result.ExitCode, 0);

            return dir;
        }
        protected static async Task<string> InitDetachedState()
        {
            string dir = await InitRepo();
            var result = await "git commit --allow-empty -m 'Second commit'; git checkout HEAD~".Execute(cwd: dir);
            Assert.IsTrue(result.StandardOutput.EndsWith("Second commit"));

            return dir;
        }
        protected static async Task<string> InitRepoWithCommit()
        {
            string dir = await InitRepoWithStagedFile();
            await "git commit -m 'contains file'".Execute(cwd: dir);

            return dir;
        }
        protected static async Task<string> InitRemoteRepo()
        {
            Git.SSH_SCRIPT = SSH_SCRIPT;
            string dir = await InitRepo();

            var sshKey = File.ReadAllLines(ssh_file.ToWindowsPath());
            Assert.AreEqual(27, sshKey.Length, delta: 1);

            // var (exitCode, stdOut, stdErr) = await $"source ../startup.sh".Execute(cwd: dir);
            var (exitCode, stdOut, stdErr) = await SSH_SCRIPT.Execute(cwd: dir);
            // (exitCode, stdOut, stdErr) = await $"source ../startup.sh  && echo '{pass}' | SSH_ASKPASS=./ap.sh ssh-add".Execute(cwd: dir);
            if (stdErr.StartsWith("@@@@@@"))
            {
                await $"sudo chmod 600 {ssh_file}".Execute(cwd: dir); // or just execute manually if I ever reinstalled Windows or something
                (exitCode, stdOut, stdErr) = await $"source ../startup.sh && ssh-add {ssh_file}".Execute(cwd: dir);
            }
            (exitCode, stdOut, stdErr) = await "git remote add origin git@github.com:JeroenBos/TestPlayground.git".Execute(cwd: dir);
            Assert.AreEqual((exitCode, stdOut, stdErr), (0, "", ""));

            try
            {
                (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git fetch".Execute(cwd: dir, cancellationToken: new CancellationTokenSource(10_000).Token);
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

            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git branch --set-upstream-to=origin/master master".Execute(cwd: dir);
            Assert.AreEqual(exitCode, 0);
            // the following depends on git version or something:
            Assert.IsTrue(stdOut.IsAnyOf("Branch 'master' set up to track remote branch 'master' from 'origin'.", 
                                         "branch 'master' set up to track 'origin/master'."), stdOut);
            Assert.AreEqual(stdErr.Split('\n').Length, 1);
            Assert.IsTrue(stdErr.StartsWith("Identity added"));

            (exitCode, stdOut, stdErr) = await $"git reset --hard {ROOT_HASH}".Execute(cwd: dir);
            Assert.AreEqual((exitCode, stdErr), (0, ""));
            Assert.AreEqual(stdOut.Split('\n').Length, 1);
            Assert.IsTrue(stdOut.StartsWith("HEAD is now at"));

            // this can throw with error containing "! [remote rejected] master -> master (cannot lock ref 'refs/heads/master'"
            // it's presumably due to parallelism, but that shouldn't be there :/
            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git fetch && git push --force-with-lease".Execute(cwd: dir);
            Assert.AreEqual((exitCode, stdOut), (0, ""), message: stdErr);
            // Assert.AreEqual(stdErr.Split('\n').Length, 2); // 3 with when (forced-updated)
            // Assert.IsTrue(stdErr.Split('\n')[1].StartsWith("Everything up-to-date"));
            return dir;
        }
        protected static async Task<string> InitRemoteRepoWithCommit(Reference<string>? commitHash = null)
        {
            string dir = await InitRemoteRepo();
            using (File.Create(Path.Combine(dir, "tmp"))) { }

            var (exitCode, stdOut, stdErr) = await "git add . && git commit -m 'first pushed file'".Execute(cwd: dir);
            Assert.AreEqual((exitCode, stdErr), (0, ""));
            (exitCode, stdOut, stdErr) = await $"{SSH_SCRIPT} && git push".Execute(cwd: dir);
            Assert.AreEqual(exitCode, 0);
            Assert.AreEqual(stdOut, "");

            if (commitHash != null)
            {
                commitHash.Value = await Git.GetCurrentHash(dir);
            }
            return dir;
        }
        protected static async Task<string> InitRemoteRepoWithRemoteCommit(Reference<string>? remoteCommitHash = null)
        {
            string dir = await InitRemoteRepoWithCommit(remoteCommitHash);

            // remove commit locally
            await "git reset --hard @~".Execute(cwd: dir);

            return dir;
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
            string dir = await InitEmptyRepo();

            bool dirty = await Git.IsDirty(dir);

            Assert.IsFalse(dirty);
        }
        [TestMethod]
        public async Task TestEmptyWithUntrackedFileIsDirty()
        {
            string dir = await InitRepoWithUntrackedFile();

            bool dirty = await Git.IsDirty(dir);

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
            string dir = await InitEmptyRepo();

            string? branchName = await Git.GetCurrentBranch(dir);

            Assert.IsTrue(branchName is null);
        }
        [TestMethod]
        public async Task Test_Get_Current_Branch()
        {
            string dir = await InitRepo();

            string? branchName = await Git.GetCurrentBranch(dir);

            Assert.IsTrue(branchName == "master" || branchName == "main");
        }
        [TestMethod]
        public async Task Test_Current_Branch_Is_None_When_In_Detached_State()
        {
            string dir = await InitDetachedState();

            string? branchName = await Git.GetCurrentBranch(dir);

            Assert.IsNull(branchName);
        }
    }
    [TestClass]
    public class GetBranchExistsTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Get_Non_Existing_Branch_Does_Not_Exit()
        {
            string dir = await InitRepo();

            bool exists = await Git.GetBranchExists(dir, "doesntexist");

            Assert.IsFalse(exists);
        }
        [TestMethod]
        public async Task Test_Current_Branch_Exists()
        {
            string dir = await InitRepo();
            string currentBranchName = (await Git.GetCurrentBranch(dir))!;

            bool exists = await Git.GetBranchExists(dir, currentBranchName);

            Assert.IsTrue(exists);
        }
    }
    [TestClass]
    public class StashTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Stash_Without_Anything_To_Stash()
        {
            string dir = await InitRepo();

            bool stashed = await Git.Stash(dir);

            Assert.IsFalse(stashed);
        }
        // TODO: [TestMethod]
        public async Task Test_Stash_With_Untracked_File_Stashes()
        {
            string dir = await InitRepoWithUntrackedFile();

            bool stashed = await Git.Stash(dir);

            Assert.IsTrue(stashed);
            Assert.IsFalse(await Git.IsDirty(dir));
        }

        // TODO: [TestMethod]
        public async Task Test_Stash_With_Tracked_File_Stashes()
        {
            string dir = await InitRepoWithStagedFile();

            bool stashed = await Git.Stash(dir);

            Assert.IsTrue(stashed);
            Assert.IsFalse(await Git.IsDirty(dir));
        }
    }
    [TestClass]
    public class PopStashTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Pop_Stash_Without_Stash_Throws()
        {
            string dir = await InitRepo();

            await Assert.ThrowsExceptionAsync<StashEmptyGitException>(async () => await Git.PopStash(dir));
        }
        [TestMethod]
        public async Task Test_Pop_Stash_Pops()
        {
            string dir = await InitRepoWithStash();

            await Git.PopStash(dir);

            Assert.IsTrue(await Git.IsDirty(dir));
        }
    }
    [TestClass]
    public class IsGitRepoTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Git_Repo_Repository_Is_Repo()
        {
            string dir = await InitRepo();

            bool isGitRepo = await Git.IsGitRepo(dir);

            Assert.IsTrue(isGitRepo);
        }
        [TestMethod]
        public async Task Test_NonGit_Repo_Repository_Is_Not_A_Repo()
        {
            string dir = IOExtensions.CreateTemporaryDirectory();

            bool isGitRepo = await Git.IsGitRepo(dir);

            Assert.IsFalse(isGitRepo);
        }
    }
    [TestClass]
    public class GetUntrackedFilesTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_Untracked_File_Is_Listed()
        {
            string dir = await InitRepoWithUntrackedFile();

            var untrackedFiles = await Git.GetUntrackedFiles(dir);

            Assert.AreEqual(1, untrackedFiles.Count);
            Assert.AreEqual("tmp", untrackedFiles[0]);
        }
        [TestMethod]
        public async Task Test_Modified_Files_Is_Not_Listed()
        {
            string dir = await InitRepoWithStagedFile();

            var untrackedFiles = await Git.GetUntrackedFiles(dir);

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
            string dir = await InitRepoWithUntrackedFile();

            // Act
            await Git.TrackAllUntrackedFiles(dir);

            // Assert
            var untrackedFiles = await Git.GetUntrackedFiles(dir);
            var stagedFiles = await Git.GetStagedFiles(dir);

            Assert.AreEqual(0, untrackedFiles.Count);
            Assert.AreEqual(0, stagedFiles.Count);
            // I don't have a method GetUnstagedFiles yet: Assert.AreEqual(1, unstagedFiles.Count);
        }
        // TODO: [TestMethod]
        public async Task Test_Tracking_Tracked_File_Is_Noop()
        {
            string dir = await InitRepoWithTrackedFile();

            // Act
            await Git.TrackAllUntrackedFiles(dir);

            // Assert
            var untrackedFiles = await Git.GetUntrackedFiles(dir);
            var stagedFiles = await Git.GetStagedFiles(dir);

            Assert.AreEqual(0, untrackedFiles.Count);
            Assert.AreEqual(0, stagedFiles.Count);
            // I don't have a method GetUnstagedFiles yet: Assert.AreEqual(1, unstagedFiles.Count);
        }
    }
    [TestClass]
    public class NewTests : GitTestsBase
    {
        [TestMethod]
        public async Task Test_New_Takes_Untracked_tracked_and_staged()
        {
            string dir = await InitRepoWithTrackedUntrackedAndStagedFiles();

            await Git.New(dir, "a", bringIndexOnly: false);

            Assert.IsTrue(File.Exists(Path.Combine(dir, "a")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "m")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "u")));
        }

        [TestMethod]
        public async Task Test_New_Only_Takes_Staged_With_Option_Index()
        {
            string dir = await InitRepoWithTrackedUntrackedAndStagedFiles("first_branch");

            await Git.New(dir, "newBranch", bringIndexOnly: true);

            Assert.IsTrue(File.Exists(Path.Combine(dir, "a")));
            Assert.IsFalse(File.Exists(Path.Combine(dir, "m")));
            Assert.IsFalse(File.Exists(Path.Combine(dir, "u")));

            // destroy state to inspect other branch state
            var (exitCode, std, err) = await @"git add . && git reset --hard && git checkout first_branch".Execute(cwd: dir);
            Assert.AreEqual(exitCode, 0);


            Assert.IsFalse(File.Exists(Path.Combine(dir, "a")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "m")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "u")));
        }

        [TestMethod]
        public async Task Test_New_Pulls_Remote()
        {
            var commitHash = new Reference<string>();
            string dir = await InitRemoteRepoWithRemoteCommit(commitHash);
            await "git checkout -b somebranch".Execute(cwd: dir);
            Assert.AreEqual(await Git.GetCurrentHash(dir), ROOT_HASH);

            await Git.New(dir, "newbranch");

            Assert.AreEqual(await Git.GetCurrentHash(dir), commitHash.Value);
        }
        [TestClass]
        public class TestGHGetPrName
        {
            // [TestMethod] // reimplement when GH login works from CI
            public async Task Test_Get_Pr_Name()
            {
                var dir = await InitRemoteRepoWithRemoteCommit();

                var branchName = await Git.GetPRBranchName(dir, "1");

                Assert.AreEqual("patch-1", branchName); 
            }
        }
        [TestClass]
        public class TestGHGetPrCommitHash
        {
            // [TestMethod] // reimplement when GH login works from CI
            public async Task Test_Get_Pr_CommitHash()
            {
                var dir = await InitRemoteRepoWithRemoteCommit();

                var branchName = await Git.GetPRBranchCommitHash(dir, "1");

                Assert.AreEqual("0b439655789e463e598535fb619a43b8bb1af8e1", branchName);
            }
        }
        [TestClass]
        public class TestGHGetPrBaseName
        {
            // [TestMethod] // reimplement when GH login works from CI
            public async Task Test_Get_Pr_BaseName()
            {
                var dir = await InitRemoteRepoWithRemoteCommit();

                var branchName = await Git.GetPRBaseBranch(dir, "1");

                Assert.AreEqual("origin/master", branchName);
            }
        }
    }
    //[TestClass]
    //public class CheckoutTests : GitTestsBase
    //{
    //	[TestMethod]
    //	public async Task Test_Simple_Checkout()
    //	{
    //	}
    //}
}
