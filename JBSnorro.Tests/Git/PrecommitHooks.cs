#nullable enable
using JBSnorro.Extensions;
using JBSnorro.Geometry;
using JBSnorro.Testing;
using JBSnorro.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace JBSnorro.Csx.Tests;

[TestClass]
/// <summary>
/// Tests the .sh file that is to be copied to individual projects.
/// </summary>
public class PrecommitHookTests : GitTestsBase
{
    protected static async Task<string> InitRepoWithPrecommithook()
    {
        string dir = await InitRepo();
        var dest = Path.Combine(dir, ".git", "hooks", "pre-commit");
        File.Copy(Path.Combine(TestProject.CurrentDirectory, "Git", "pre-commit.sh"), dest);
        await $"sudo chmod +x {dest}".Execute(cwd: dir);

        return dir;
    }
    protected static async Task<string> InitRepoWithStagedFileWithMissingOEFNewLine(bool longText = false)
    {
        string dir = await InitRepoWithPrecommithook();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), (longText ? "first line\n\n\n\n\n\n\n\n\n\n" : "" ) + "line without new line");

        await "git add .".Execute(cwd: dir);
        return dir;
    }

    protected static async Task<string> InitRepoWithStagedFileWithMissingOEFNewLineAndUnstagedChange(bool longText = false)
    {
        string dir = await InitRepoWithStagedFileWithMissingOEFNewLine();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), (longText ? "first line\n\n\n\n\n\n\n\n\n\n" : "") + "line without new line");

        await "git add .".Execute(cwd: dir);
        return dir;
    }
    protected static async Task<string> InitRepoWithStagedFileWithOEFLine()
    {
        string dir = await InitRepoWithPrecommithook();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), "line with EOF new line\n");

        await "git add .".Execute(cwd: dir);
        return dir;
    }
    protected static async Task<string> InitRepoWithStagedFileWithStagedCRLFFile(bool longText = false)
    {
        string dir = await InitRepoWithPrecommithook();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), Enumerable.Range(0, longText ? 10 : 2).Select(i => $"line{i}\r\n").Join(""));

        await "git add .".Execute(cwd: dir);
        Assert.IsTrue(File.ReadAllText(Path.Combine(dir, "tmp.txt")).Contains('\r'));
        return dir;
    }
    protected static async Task<string> InitRepoWithStagedFileWithStagedCRLFFileAndUnstagedChange(bool longText = false)
    {
        string dir = await InitRepoWithStagedFileWithStagedCRLFFile(longText);

        string file = Path.Combine(dir, "tmp.txt");
        // add something to working tree that won't result in a merge conflict:
        File.WriteAllText(file, "zeroth line\n" + File.ReadAllText(file));

        return dir;
    }
    protected static async Task<IAsyncDisposable> DisableGitConfigAutoCRLF()
    {
        var currentValue = (await "git config core.autocrlf".Execute()).StandardOutput.Trim();
        await $"git config set core.autocrlf false".Execute();
        return Disposable.Create(async () => await $"git config set core.autocrlf {currentValue}".Execute());
    }

    [TestMethod]
    public async Task CheckPrecommitScriptAddsEOF()
    {
        var gitDir = await InitRepoWithStagedFileWithMissingOEFNewLine();

        Console.WriteLine("Act");
        var x = await "bash ./.git/hooks/pre-commit".Execute(cwd: gitDir);
        Console.WriteLine(x.ErrorOutput);

        Console.WriteLine("----------------");
        Console.WriteLine("StandardOutput:");
        Console.WriteLine(x.StandardOutput);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line without new line\n", text);
    }
    [TestMethod]
    public async Task CheckPrecommitScriptDoesntAddEOFIfAlreadyThere()
    {
        var gitDir = await InitRepoWithStagedFileWithOEFLine();

        await "bash ./.git/hooks/pre-commit".Execute(cwd: gitDir);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line with EOF new line\n", text);
    }
    [TestMethod]
    public async Task CheckPrecommitHookAddsEOF()
    {
        var gitDir = await InitRepoWithStagedFileWithMissingOEFNewLine();

        var x = await "git commit -m 'commit'".Execute(cwd: gitDir);
        Console.WriteLine(x.ErrorOutput);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        TestExtensions.AreEqual(expected: "line without new line\n", text);
    }

    [TestMethod]
    public async Task AddingOEFLFDoesntCommitWorkingTree()
    {
        // Arrange
        var gitDir = await InitRepoWithStagedFileWithMissingOEFNewLine(longText: true);
        string file = Path.Combine(gitDir, "tmp.txt");
        // add something to working tree that won't result in a merge conflict:
        File.WriteAllText(file, "zeroth line\n" + File.ReadAllText(file));

        // Act
        var x = await "git commit -m 'commit'".Execute(cwd: gitDir);
        Console.WriteLine(x.ErrorOutput);

        var text = File.ReadAllText(file);
        Assert.AreEqual(expected: "zeroth line\nfirst line\n\n\n\n\n\n\n\n\n\nline without new line", text);
        var diff = await "git diff".Execute(cwd: gitDir);
        Assert.IsTrue(diff.StandardOutput.Contains("+zeroth line"));
    }



    [TestMethod]
    public async Task CheckPrecommitScriptReplacesCRLFWithLF()
    {
        var gitDir = await InitRepoWithStagedFileWithStagedCRLFFile();

        var x = await "bash ./.git/hooks/pre-commit".Execute(cwd: gitDir);
        Console.WriteLine(x.ErrorOutput);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line0\nline1\n", text);
    }
    [TestMethod]
    public async Task CheckPrecommitHookReplacesCRLFWithLF()
    {
        var gitDir = await InitRepoWithStagedFileWithStagedCRLFFile();

        await "git commit -am 'commit'".Execute(cwd: gitDir);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line0\nline1\n", text);
    }

    [TestMethod]
    public async Task ConvertingCRLFDoesntCommitWorkingTree()
    {
        // Arrange
        var gitDir = await InitRepoWithStagedFileWithStagedCRLFFileAndUnstagedChange(longText: true);
        
        // Act
        var x = await "git commit -m 'commit'".Execute(cwd: gitDir);
        Console.WriteLine(x.ErrorOutput);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "zeroth line\nfirst line\n\n\n\n\n\n\n\n\n\nline without new line", text);
        var diff = await "git diff".Execute(cwd: gitDir);
        Assert.IsTrue(diff.StandardOutput.Contains("+zeroth line"));
    }

}