using JBSnorro.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using System.Collections.Generic;

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
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path)) { EnableRaisingEvents = true };
        watcher.Changed += (sender, e) => yield();
        watcher.Error += (sender, e) => { error = "error"; dispose(); };
        watcher.Deleted += (sender, e) => { error = "deleted"; dispose(); };
        watcher.Disposed += (sender, e) => { error = "disposed"; dispose(); };
        watcher.Renamed += (sender, e) => { error = "renamed"; dispose(); };


        done ??= new Reference<bool>();
        var streamPosition = new Reference<long>();

        await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken))
            yield return line;

        await foreach (var _ in everyFileChange)
            await foreach (var line in ReadAllLinesContinuouslyInProcess(path, streamPosition, done, cancellationToken))
                yield return line;

        if (error != null)
            throw new Exception(error);
    }
    //class Wrapper<T> : IAsyncEnumerator<T>, IAsyncDisposable
    //{
    //    private readonly IAsyncEnumerator<T> enumerator;
    //    private ValueTask<bool>? last = default;
    //    public Wrapper(IAsyncEnumerator<T> enumerator)
    //    {
    //        this.enumerator = enumerator;
    //    }
    //    public T Current => enumerator.Current;
    //    public ValueTask<bool> MoveNextAsync()
    //    {
    //        Console.WriteLine("MoveNextAsync");
    //        last = enumerator.MoveNextAsync();
    //        return last.Value;
    //    }
    //    public ValueTask DisposeAsync()
    //    {
    //        Console.WriteLine("DisposeAsync");
    //        return ((IAsyncDisposable)enumerator).DisposeAsync();
    //    }
    //}
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
        CancellationToken cancellationToken = default)
    {
        Diagnostics.Contract.Requires(maxChunkSize > 0);

        var enumerable = ReadAllLinesContinuously(path, done, cancellationToken);
        
        Task<bool>? moveNextTask = null;
        var lines = new List<string>();

        return new StateMachine(lines, moveNextTask, enumerable, maxChunkSize, cancellationToken);


        // the below algorithm has been encoded into a state machine:
        //while (true)
        //{
        //    if (moveNextTask != null)
        //    {
        //        if (!await enumerator.MoveNextAsync())
        //            break;
        //        lines.Add(enumerator.Current);
        //        moveNextTask = null;
        //    }

        //    var task = enumerator.MoveNextAsync();
        //    if (task.IsCompleted)
        //    {
        //        if (!await task) break;
        //        lines.Add(enumerator.Current);
        //        if (lines.Count != maxChunkSize)
        //        {
        //            continue;
        //        }
        //    }
        //    else
        //    {
        //        moveNextTask = task.AsTask();
        //        if (lines.Count == 0)
        //        {
        //            continue;
        //        }
        //    }
        //    yield return lines;
        //    lines.Clear();
        //}
        //if (lines.Count != 0)
        //{
        //    yield return lines;
        //}
    }

    class StateMachine : IAsyncDisposable, IAsyncEnumerable<List<string>>, IAsyncEnumerator<List<string>>
    {
        private bool disposed;
        private int state = -1;
        private ConfiguredValueTaskAwaitable<bool> currentTask;
        private bool isDoingMoveNextAlready;
        private CancellationTokenSource cts;

        // captured variables        
        private List<string> lines;
        private Task<bool>? moveNextTask;
        private IAsyncEnumerator<string> enumerator;
        private int maxChunkSize;
        public StateMachine(List<string> lines, Task<bool>? moveNextTask, IAsyncEnumerable<string> enumerable, int maxChunkSize, CancellationToken cancellationToken)
        {
            this.cts = new CancellationTokenSource();
            this.lines = lines;
            this.moveNextTask = moveNextTask;
            this.enumerator = enumerable.GetAsyncEnumerator(cts.Token);
            this.maxChunkSize = maxChunkSize;
        }
        public List<string> Current => lines;
        private ConfiguredValueTaskAwaitable<bool>? Do() {
            bool first = true;

            while (true)
            {
                if (disposed) {
                    state = 6;
                    return null;
                }
                if (first)
                {
                    first = false;
                    switch(this.state)
                    {
                        case -1:
                            break;
                        case 0:
                            throw new Exception("Unreachable");
                        case 1:
                            goto one;
                        case 2:
                            goto two;
                        case 3:
                            goto three;
                        case 4:
                            goto four;
                        case 5:
                            throw new Exception("Enumerator finished");
                        case 6:
                            throw new Exception("Enumerator disposed");
                        default:
                            throw new Exception("Unreachable");
                    }
                }
                if (moveNextTask != null)
                {
                    this.currentTask = enumerator.MoveNextAsync().ConfigureAwait(false);
                    state = 1;
                    return this.currentTask;
                }
                goto skipOne;
            one:
                if (!currentTask.GetAwaiter().GetResult())
                    break;
                lines.Add(enumerator.Current);
                moveNextTask = null;
            skipOne:

                var task = enumerator.MoveNextAsync();
                if (task.IsCompleted)
                {
                    this.currentTask = task.ConfigureAwait(false);
                    state = 2;
                    return this.currentTask;
                    // label two would be here
                }
                else
                {
                    moveNextTask = task.AsTask();
                    if (lines.Count == 0)
                    {
                        continue;
                    }
                }
                goto skipTwo;
            two:
                Diagnostics.Contract.Assert(this.currentTask.GetAwaiter().IsCompleted);
                if (!this.currentTask.GetAwaiter().GetResult()) break;
                lines.Add(enumerator.Current);
                if (lines.Count != maxChunkSize)
                {
                    continue;
                }
            skipTwo:
                state = 3;
                return null;
            three: ;
                lines.Clear();
            }
            if (lines.Count != 0)
            {
                state = 4;
                return null;
            }
            four: ;
            state = 5;
            return null;
        }


        public async ValueTask<bool> MoveNextAsync()
        {
            if (isDoingMoveNextAlready) throw new Exception("A MoveNextAsync is already in progress");

            isDoingMoveNextAlready = true;
            try
            {
                while(true) 
                {
                    var task = Do();
                    if (this.disposed) {
                        Console.WriteLine("Disposed of while running");
                        await enumerator.DisposeAsync();
                        return false;
                    }
                    if (task == null) 
                    {
                        isDoingMoveNextAlready = false;
                        // means we can yield or we have finished
                        if (state == 3 || state == 4)
                        {
                            return true;
                        }
                        return false;
                    }
                    await task.Value;
                }
            }
            catch
            {
                isDoingMoveNextAlready = false;
                throw;
            }
            finally
            {
                if (this.disposed) {
                    await enumerator.DisposeAsync();
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            this.disposed = true;
            if (!this.isDoingMoveNextAlready)
            {
                return enumerator.DisposeAsync();
            }
            return ValueTask.CompletedTask;
        }

        private bool isEnumerating = false;
        public IAsyncEnumerator<List<string>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (isEnumerating)
                throw new InvalidOperationException("GetAsyncEnumerator already called");
            isEnumerating = true;
            return this;
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
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sr = new StringReaderThatYieldsWholeLines(fs);

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
