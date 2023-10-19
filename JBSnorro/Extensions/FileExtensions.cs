using JBSnorro.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace JBSnorro.Extensions;

public static class FileExtensions
{
    /// <summary>
    /// Continuously reads all lines of a file and yields are lines written to it by other processes.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static async IAsyncEnumerable<string> ReadAllLinesContinuously(
        string path,
        Reference<bool>? done = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (Directory.Exists(path)) throw new ArgumentException("Cannot specify a directory", nameof(path));
        if (!File.Exists(path)) throw new ArgumentException("File doesn't exist", nameof(path));


        IAsyncEnumerable<object?> everyFileChange = IAsyncEnumerableExtensions.Create(out var yield, out var dispose);
        string? error = null;
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName
        };
        watcher.Changed += (sender, e) => yield();
        watcher.Error += (sender, e) => { error = "error"; dispose(); };
        watcher.Deleted += (sender, e) => { error = "deleted"; dispose(); };
        watcher.Disposed += (sender, e) => { error = "disposed"; dispose(); };
        watcher.Renamed += (sender, e) => { error = "renamed"; dispose(); };

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        // HACK: I have absolutely no clue, but if I set EnableRaisingEvents to true in a task, it seems to work; otherwise it doesn't. I'm suspecting a deadlock somewhere but I can't find it.
        Task.Run(async () => { await Task.Delay(1); watcher.EnableRaisingEvents = true; }, cancellationToken);
#pragma warning restore CS4014

        done ??= new Reference<bool>();
        var streamPosition = new Reference<long>();

        await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken).ConfigureAwait(false))
            yield return line;

        await foreach (var _ in everyFileChange)
            await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken).ConfigureAwait(false))
                yield return line;

        if (error != null)
            throw new Exception(error);
    }

    /// <summary>
    /// Continuously reads all lines of a file and yields are lines written to it by other processes.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static IAsyncEnumerable<IReadOnlyCollection<string>> ReadAllLinesChunkedContinuously(
        string path,
        Reference<bool>? done = null,
        int maxChunkSize = 100,
        int blocked_ms = 10,
        CancellationToken cancellationToken = default)
    {
        Diagnostics.Contract.Requires(maxChunkSize > 0);

        return ReadAllLinesContinuously(path, done, cancellationToken).Buffer(maxChunkSize, blocked_ms, cancellationToken);
    }
    /// <summary>
    /// Continuously reads all lines of a file and yields are lines written to it within this process (or so it appears to work).
    /// Other processes are allowe to still write to it, but won't be reflected.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    private static async IAsyncEnumerable<string> ReadAllLinesContinuouslyInProcess(
         string path,
         Reference<long> readStreamPosition,
         Reference<bool> done,
         [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StringReaderThatYieldsWholeLines(fs);

        fs.Position = readStreamPosition.Value;
        long stringBuilderStartPosition = -1;
        StringBuilder? stringBuilder = null;
        while (true)
        {
            long streamPositionBeforeCurrentRead = sr.BaseStream.Position;
            string? line;
            try
            {
                line = await sr.ReadLineAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (done.Value == true)
                    break;
                else
                    throw;
            }
            finally
            {
                readStreamPosition.Value = sr.BaseStream.Position;
            }
            if (!string.IsNullOrEmpty(line))
            {
                if (sr.LastCharacterWasNewLine)
                {
                    if (stringBuilder == null)
                    {
                        yield return line;
                    }
                    else
                    {
                        stringBuilder.Append(line);
                        yield return stringBuilder.ToString();
                        stringBuilder = null;
                        stringBuilderStartPosition = -1;
                    }
                }
                else
                {
                    stringBuilder ??= new StringBuilder();
                    stringBuilderStartPosition = streamPositionBeforeCurrentRead;
                    stringBuilder.Append(line);
                }
            }
            else if (line == null)
            {
                if (stringBuilder != null)
                {
                    readStreamPosition.Value = stringBuilderStartPosition;
                }
                yield break;
            }
            else if (done.Value == true)
            {
                break;
            }
            else
            {
                try
                {
                    await Task.Delay(10, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    if (done.Value == true)
                        break;
                    else
                        throw;
                }
            }
        }
    }
}
