﻿using JBSnorro.Csx;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Logging;
using static JBSnorro.Extensions.TaskExtensions;

namespace JBSnorro.IO;

public static class TempFileCleanup
{
    private static ILogger? logger = null!; // = ILogger.CreateFileLogger(EnvironmentExtensions.GetDebugOutputPath);
    /// <summary>
    /// Gets whether the environment is configured for temporary file cleanup.
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            bool ci = EnvironmentExtensions.IsCI;
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
    public static DisposableTaskOutcome? Register(string path, int lifetime_minutes = 60)
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

        logger?.LogInfo($"{fullpath}: explicitly cleaning up");
        return CleanupLine(ToLine(fullpath, TimeSpan.Zero), ignoreTimestamp: true);
    }
    public static IDisposable CreateTempFile(out string path, string extension = "")
    {
        if (string.IsNullOrEmpty(extension))
        {
            path = Path.GetTempFileName();
        }
        else
        {
            path = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString() + "." + extension);
            using (var writer = File.Create(path))
            {
                // just touching the file
            }
        }
        return Register(path) as IDisposable ?? Disposable.Empty;
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
        Task Append(int attempt)
        {
            logger?.LogDebug($"{path}: Appending line (#{attempt})");
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
                logger?.LogDebug($"{path}: Was already deleted");
                return true;
            }
            if (!ignoreTimestamp && DateTime.UtcNow < expirationTime)
            {
                logger?.LogDebug($"{path}: Not ready for deletion (ETA {(expirationTime - DateTime.UtcNow).ToNiceString()})");
                return false;
            }

            if (path.EndsWith('/'))
            {
                logger?.LogDebug($"{path}: Going to delete directory");
                Directory.Delete(path, recursive: true);
                logger?.LogInfo($"{path}: Successfully deleted directory");
            }
            else
            {
                logger?.LogDebug($"{path}: Going to delete file");
                File.Delete(path);
                logger?.LogInfo($"{path}: Successfully deleted file");
            }
            return true;
        }
        catch (UnauthorizedAccessException ex) when (path != null && ex.Message.StartsWith("Access to the path '"))
        {
            logger?.LogError($"{path}: UnauthorizedAccessException in deleting");
            string fileName = ex.Message["Access to the path '".Length..].SubstringUntil("'");

            if (recursing)
            {
                string[] paths = Directory.GetFiles(path, fileName, SearchOption.AllDirectories).Map(filename => Path.Join(path, filename));

                if (paths.Length == 0)
                {
                    logger?.LogWarning($"{path}: Don't have access and can't even find path");
                    return false;
                }

                string filePath = Path.Combine(path, paths[0]);
                LogDebug(filePath);
                return false;
            }
            else
            {
                try
                {
                    // -R is recursive
                    // 701 is full control to everybody
                    logger?.LogInfo($"{path}: chmodding");
                    await $"chmod -R 701 '{path}'".Execute();
                    logger?.LogInfo($"{path}: chmodded");
                }
                catch
                {
                    logger?.LogError($"{path}: chmod failed");
                    return false;
                }

                logger?.LogInfo($"{path}: chmod succeeded. Recursing");
                return await CleanupLine(line, ignoreTimestamp, recursing: true);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError($"'{path}': deleting resulted in {ex.GetType().Name}.\n{ex.Message}");
            Console.WriteLine(ex.Message);
            return false;
        }

        static void LogDebug(string filePath)
        {
            var processes = ProcessExtensions.GetLockingProcesses(filePath);
            logger?.LogDebug($"{filePath}: Access denied");
            logger?.LogDebug($"These process IDS are locking it (Count = {processes.Count}):");
            foreach (var lockingProcess in processes)
            {
                logger?.LogDebug("- " + lockingProcess.Id.ToString());
            }
        }
    }
    private static async Task Cleanup(string configPath, int lifetime_minutes)
    {
        logger?.LogInfo("Starting cleanup");
        string[] lines;
        try
        {
            lines = await ReadLinesAndClear(configPath);
        }
        catch
        {
            logger?.LogError("Error reading cleanup config");
            return; // couldn't do clean up; just skip it
        }

        logger?.LogInfo($"Cleanup config contained {lines.Length} lines");
        // this section shouldn't be able to crash
        var removalTasks = lines.Map(line => CleanupLine(line, ignoreTimestamp: false));
        await Task.WhenAll(removalTasks);
        var notRemovedLines = lines.Zip(removalTasks)
                                   .Where(_ => !_.Second.Result)
                                   .Select(_ => _.First)
                                   .ToList();
        logger?.LogInfo($"Cleanup config: {notRemovedLines.Count} lines were't removed");

        if (notRemovedLines.Count != 0)
        {
            await Retry(RestoreRemainder);
        }
        async Task RestoreRemainder()
        {
            logger?.LogDebug("Reappending those lines");
            await File.AppendAllLinesAsync(configPath, notRemovedLines);
            logger?.LogDebug("Reappended those lines");
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

