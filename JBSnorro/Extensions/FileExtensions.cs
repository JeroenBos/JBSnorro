using JBSnorro.Diagnostics;
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
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path)) { EnableRaisingEvents = true };
        watcher.Changed += fileChanged;
        watcher.Error += (sender, e) => dispose();
        watcher.Deleted += (sender, e) => dispose();
        watcher.Disposed += (sender, e) => dispose();


        done ??= new Reference<bool>();
        var streamPosition = new Reference<long>();

        await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken))
            yield return line;

        await foreach (var _ in everyFileChange)
            await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken))
                yield return line;

        void fileChanged(object sender, FileSystemEventArgs e)
        {
            yield();
        }
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
