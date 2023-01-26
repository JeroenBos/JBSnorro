#nullable enable
using JBSnorro.Csx;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using static JBSnorro.Extensions.TaskExtensions;

namespace JBSnorro.IO;

public static class TempFileCleanup
{
    /// <summary>
    /// Gets whether the environment is configured for temporary file cleanup.
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            bool ci = bool.Parse(Environment.GetEnvironmentVariable("CI") ?? "false");
            bool enabled = !ci;
            return enabled;
        }
    }

    /// <summary>
    /// Registers the path of a directory or file that exists temporarily. 
    /// Disposing of the returned disposable will delete it, as well as other temporarily files that are created on previous runs.
    /// </summary>
    /// <param name="path">The path of the temporary file or directory that was created. </param>
    /// <param name="lifetime_minutes">The lifetime in minutes after which other processes may clean up the path.</param>
    public static IAsyncDisposable? Register(string path, int lifetime_minutes = 60)
    {
        if (Path.Exists(path))
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                Contract.Requires(path.EndsWith('/'), "Directories must end on a directory separator");
            }
            else
            {
                Contract.Requires(!path.EndsWith('/'), "Files must not end on a directory separator");
            }
        }
        else
        {
            // we can't check trailing character correctness and just assume it's been provided correctly
        }

        if (!IsEnabled)
        {
            return null; // No need to do cleanup in CI
        }

        var configPath = Environment.GetEnvironmentVariable("TEMP_FILE_REGISTRY_PATH") ?? Path.Combine(Path.GetTempPath(), ".temp_file_registry.txt");

        return new DisposableTaskOutcome(
            task: asyncPart(),
            disposable: new AsyncDisposable(() => Unregister(path))
        );

        async Task asyncPart()
        {
            await AppendLine(configPath, path, TimeSpan.FromMinutes(lifetime_minutes));
            await Cleanup(configPath, lifetime_minutes);
        }
    }
    /// <summary>
    /// Removes the directory or file at the specified path. Disregards any lifetime that path was attributed.
    /// </summary>
    /// <param name="path">The path of the temporary file or directory that was created. </param>
    public static Task Unregister(string fullpath)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask; // No need to do cleanup in CI
        }

        return CleanupLine(ToLine(fullpath, TimeSpan.Zero), ignoreTimestamp: true);
    }

    private static Task<string[]> ReadLines(string configPath)
    {
        return Retry(Append);
        Task<string[]> Append()
        {
            return File.ReadAllLinesAsync(configPath);
        }
    }
    private static Task<string[]> ReadLinesAndClear(string configPath)
    {
        return Retry(Append);
        async Task<string[]> Append()
        {
            using FileStream file = File.Open(configPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using StreamReader reader = new StreamReader(file);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                lines.Add((await reader.ReadLineAsync())!);
            }
            file.SetLength(0); // from now on if this process were to die, there's no backup of the lines. TODO: fix
            return lines.ToArray();
        }
    }
    private static Task AppendLine(string configPath, string path, TimeSpan lifetime)
    {
        string line = ToLine(path, lifetime);
        return Retry(Append);
        Task Append()
        {
            return File.AppendAllLinesAsync(configPath, new[] { line });
        }
    }

    /// <returns>whether the file or directory described by the line was successfully removed or was already not present.</returns>
    private static async Task<bool> CleanupLine(string line, bool ignoreTimestamp, bool recursing = false)
    {
        string? path = null;
        try
        {
            var (relativePath, expirationTime) = ToPath(line);
            path = Path.Join(Path.GetTempPath(), relativePath);
            if (!Path.Exists(path))
            {
                return true;
            }
            if (!ignoreTimestamp && DateTime.UtcNow < expirationTime)
            {
                return false;
            }

            if (path.EndsWith('/'))
            {
                Directory.Delete(path, recursive: true);
            }
            else
            {
                File.Delete(path);
            }
            return true;
        }
        catch (UnauthorizedAccessException ex) when (path != null && ex.Message.StartsWith("Access to the path '"))
        {
            string fileName = ex.Message["Access to the path '".Length..].SubstringUntil("'");

            if (recursing)
            {
                string[] paths = Directory.GetFiles(path, fileName, SearchOption.AllDirectories).Map(filename => Path.Join(path, filename));

                if (paths.Length == 0)
                {
                    // don't have access and can't even find path
                    return false;
                }

                // just outputting some debug info here:

                string filePath = Path.Combine(path, paths[0]);
                Console.WriteLine($"Access to the path '{filePath}' denied");
                Console.Write("These process IDS are locking it ");

                var processes = ProcessExtensions.GetLockingProcesses(filePath);
                Console.WriteLine($"(Count = {processes.Count}):");
                foreach (var lockingProcess in processes)
                {
                    Console.Write("- ");
                    Console.WriteLine(lockingProcess.Id.ToString());
                }
                return false;
            }
            else
            {
                // -R is recursive
                // 701 is full control to everybody
                try
                {
                    await $"chmod -R 701 '{path}'".Execute();
                }
                catch
                {
                    return false;
                }

                return await CleanupLine(line, ignoreTimestamp, recursing: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
    private static async Task Cleanup(string configPath, int lifetime_minutes)
    {
        string[] lines;
        try
        {
            lines = await ReadLinesAndClear(configPath);
        }
        catch
        {
            return; // couldn't do clean up; just skip it
        }

        // this section shouldn't be able to crash
        var removalTasks = lines.Map(line => CleanupLine(line, ignoreTimestamp: false));
        await Task.WhenAll(removalTasks);
        var notRemovedLines = lines.Zip(removalTasks)
                                   .Where(_ => !_.Second.Result)
                                   .Select(_ => _.First)
                                   .ToList();
        if (notRemovedLines.Count != 0)
        {
            await Retry(RestoreRemainder);
        }
        async Task RestoreRemainder()
        {
            await File.AppendAllLinesAsync(configPath, notRemovedLines);
        }
    }

    private static string ToLine(string fullpath, TimeSpan lifetime)
    {
        string pathPart;
        if (fullpath.EndsWith('/'))
        {
            pathPart = Path.GetDirectoryName(fullpath)!;
            Contract.Assert(pathPart.StartsWith(Path.GetTempPath()));
            pathPart = pathPart[Path.GetTempPath().Length..];
            pathPart = pathPart.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); // mostly for TrimStart
            pathPart += "__DIR__";
        }
        else
        {
            pathPart = Path.GetFileName(fullpath);

        }
        return $"{DateTime.UtcNow + lifetime:yyyy-MM-dd HH:mm:ss}Z___{pathPart}";
    }
    private static (string Path, DateTime ExpirationTime) ToPath(string line)
    {
        const string separator = "___";
        int separatorIndex = line.IndexOf(separator);
        string path = line[(separatorIndex + separator.Length)..].Replace("__DIR__", "/");
        DateTime expirationTime = DateTime.ParseExact(line[..separatorIndex], "yyyy-MM-dd HH:mm:ssZ", null, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
        return (path, expirationTime);
    }
}

