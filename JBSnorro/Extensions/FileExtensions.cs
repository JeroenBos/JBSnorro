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
    /// <param name="enableDelayed">A boolean indicating whether the watcher should be enabled with a delay.</param>
    /// There is this horrible behavior in the consuming application that the watcher.Changed event handler does not trigger unless
    /// I enable the watcher with a delay. I have no clue what's going on.
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static async IAsyncEnumerable<string> ReadAllLinesContinuously(
        string path,
        bool enableDelayed = false,
        Reference<bool>? done = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (Directory.Exists(path)) throw new ArgumentException("Cannot specify a directory", nameof(path));
        if (!File.Exists(path)) throw new ArgumentException("File doesn't exist", nameof(path));


        var onEveryFileChange = IAsyncEnumerableExtensions.Create(out var yield, out var dispose);
        string? error = null;
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path));
        watcher.Changed += (sender, e) => yield();
        watcher.Error += (sender, e) => { error = "error"; dispose(); };
        watcher.Deleted += (sender, e) => { error = "deleted"; dispose(); };
        watcher.Disposed += (sender, e) => { error = "disposed"; dispose(); };
        watcher.Renamed += (sender, e) => { error = "renamed"; dispose(); };
        watcher.SetEnabledRaisingEvents(enableDelayed, cancellationToken);

        done ??= new Reference<bool>();
        var streamPosition = new Reference<long>();

        await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken).ConfigureAwait(false))
            yield return line;

        await foreach (var _ in onEveryFileChange())
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
    /// <param name="maxChunkSize">The maximum number of elements to be returned by the list.</param>
    /// <param name="blocked_ms">The number of milliseconds to wait for an item element before yielding the current buffer (if non-empty).</param>
    /// <param name="yield_every_ms">The maximum number of milliseconds in between yields of the buffer (if non-empty).</param>
    /// <returns>Any yielded <c>List&lt;T&gt;</c> will be reused.</returns>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static IAsyncEnumerable<IReadOnlyCollection<string>> ReadAllLinesChunkedContinuously(
        string path,
        bool enabledWatcherDelayed = false,
        Reference<bool>? done = null,
        int maxChunkSize = 100,
        int blocked_ms = 10,
        int yield_every_ms = 100,
        CancellationToken cancellationToken = default)
    {
        Diagnostics.Contract.Requires(maxChunkSize > 0);

        return ReadAllLinesContinuously(path, enabledWatcherDelayed, done, cancellationToken).Buffer(maxChunkSize, blocked_ms, yield_every_ms, cancellationToken);
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

    private static void SetEnabledRaisingEvents(this FileSystemWatcher watcher, bool delayed, CancellationToken cancellationToken)
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        if (delayed)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1);
                watcher.EnableRaisingEvents = true;
            }, cancellationToken);
        }
        else
        {
            watcher.EnableRaisingEvents = true;
        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}
