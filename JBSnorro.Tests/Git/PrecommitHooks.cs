#nullable enable
using JBSnorro.Extensions;
using JBSnorro.Geometry;
using JBSnorro.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    protected static async Task<string> InitRepoWithStagedFileWithMissingOEFNewLine()
    {
        string dir = await InitRepoWithPrecommithook();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), "line without new line");

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
    protected static async Task<string> InitRepoWithStagedFileWithStagedCRLFFile()
    {
        string dir = await InitRepoWithPrecommithook();
        File.WriteAllText(Path.Combine(dir, "tmp.txt"), "line1\r\nline2\r\n");

        await "git add .".Execute(cwd: dir);
        return dir;
    }

    [TestMethod]
    public async Task CheckPrecommitScriptAddsEOF()
    {
        var gitDir = await InitRepoWithStagedFileWithMissingOEFNewLine();

        await "bash ./.git/hooks/pre-commit".Execute(cwd: gitDir);

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

        await "git commit -am 'commit'".Execute(cwd: gitDir);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line without new line\n", text);
    }




    [TestMethod]
    public async Task CheckPrecommitScriptReplacesCRLFWithLF()
    {
        var gitDir = await InitRepoWithStagedFileWithStagedCRLFFile();

        await "bash ./.git/hooks/pre-commit".Execute(cwd: gitDir);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line1\nline2\n", text);
    }
    [TestMethod]
    public async Task CheckPrecommitHookReplacesCRLFWithLF()
    {
        var gitDir = await InitRepoWithStagedFileWithStagedCRLFFile();

        await "git commit -am 'commit'".Execute(cwd: gitDir);

        var text = File.ReadAllText(Path.Combine(gitDir, "tmp.txt"));
        Assert.AreEqual(expected: "line1\nline2\n", text);
    }
}