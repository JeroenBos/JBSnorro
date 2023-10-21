using JBSnorro.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace JBSnorro.Extensions;

public static class FileExtensions
{
    static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    static readonly ThreadLocal<List<(long, string)>> written = new ThreadLocal<List<(long, string)>>(() => new List<(long, string)>(), true);
    public static void WriteLine(string s)
    {
        Write(s + "\n");
    }
    public static void Write(string s)
    {
        written.Value!.Add((Stopwatch.ElapsedTicks, s));
    }
    public static List<string> GetWritings()
    {
        var writings = written.Values.SelectMany(value => value).OrderBy(_ => _.Item1).Select(_ => _.Item2).ToList();
        return writings;
    }
    public static void ClearWritings()
    {
        foreach (var list in written.Values)
        {
            list.Clear();
        }
    }
    public static IAsyncEnumerable<string> ReadAllLinesContinuously(string path, Reference<bool>? done = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return _ReadAllLinesContinuously(path, done, cancellationToken);
        }
        finally
        {
            WriteLine("looping exit");
        }
    }
    /// <summary>
    /// Continuously reads all lines of a file and yields are lines written to it by other processes.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static async IAsyncEnumerable<string> _ReadAllLinesContinuously(
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
            EnableRaisingEvents = true,
        };
        watcher.Changed += (sender, e) => { WriteLine("yielding ping"); yield(); WriteLine("yielding pong"); };
        watcher.Error += (sender, e) => { error = "error"; dispose(); };
        watcher.Deleted += (sender, e) => { error = "deleted"; dispose(); };
        watcher.Disposed += (sender, e) => { error = "disposed"; dispose(); };
        watcher.Renamed += (sender, e) => { error = "renamed"; dispose(); };

        done ??= new Reference<bool>();
        var streamPosition = new Reference<long>();

        await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken).ConfigureAwait(false))
        {
            WriteLine("yielding a ping");
            yield return line;
            WriteLine("yielding a pong");

        }

        WriteLine("Going to await everyFileChange");
        await foreach (var _ in everyFileChange)
        {
            WriteLine("in foreach from yield()");
            await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken).ConfigureAwait(false))
            {
                WriteLine("in inner foreach from yield()");
                yield return line;
                WriteLine("in inner foreach from yield() pong");
            }
        }
        WriteLine("EXITED");

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
        Reference<bool>? done = null,
        int maxChunkSize = 100,
        int blocked_ms = 10,
        int yield_every_ms = 100,
        CancellationToken cancellationToken = default)
    {
        Diagnostics.Contract.Requires(maxChunkSize > 0);

        return ReadAllLinesContinuously(path, done, cancellationToken).Buffer(maxChunkSize, blocked_ms, yield_every_ms, cancellationToken);
    }
    private static async IAsyncEnumerable<string> ReadAllLinesContinuouslyInProcess(string path, Reference<long> readStreamPosition, Reference<bool> done, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var item in _ReadAllLinesContinuouslyInProcess(path, readStreamPosition, done, cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            WriteLine("Finished ReadAllLinesContinuouslyInProcess");
        }
    }
    /// <summary>
    /// Continuously reads all lines of a file and yields are lines written to it within this process (or so it appears to work).
    /// Other processes are allowe to still write to it, but won't be reflected.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    private static async IAsyncEnumerable<string> _ReadAllLinesContinuouslyInProcess(
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
            WriteLine("looping");
            long streamPositionBeforeCurrentRead = sr.BaseStream.Position;
            string? line;
            try
            {
                WriteLine("before sr.ReadLineAsync");
                line = await sr.ReadLineAsync(cancellationToken);
                WriteLine($"after sr.ReadLineAsync: {(line is null ? "null" : $"'{line}'")}");
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
                        WriteLine("Yielding line a");
                        yield return line;
                        WriteLine("Yielded line a");
                    }
                    else
                    {
                        stringBuilder.Append(line);
                        WriteLine("Yielding line b");
                        yield return stringBuilder.ToString();
                        WriteLine("Yielded line b");
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
