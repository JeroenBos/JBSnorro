using JBSnorro;
using JBSnorro.Collections.Immutable;
using System.Text;

namespace JBSnorro.Logging;

public interface ILogger
{
	public static ILogger Create()
	{
		return new Logger();
	}
	public static ILogger Create(EntryWrittenEventHandler eventHandler)
	{
		var logger = new Logger();
		logger.EntryWritten += eventHandler;
		return logger;
	}
	public static ILogger CreateConsoleLogger()
	{
		return Create(eventHandler);
		static void eventHandler(object sender, EntryWrittenEventArgs e)
		{
			var timestamp = $"{e.Entry.Timestamp:yyyy-MM-dd HH:mm:ss:fffff}";
			Console.Write(timestamp);
			Console.Write(" ");
			Console.Write(e.Entry.Type.ToJustifiedString()); // ends on ':'
			Console.Write(" ");
            Console.WriteLine(e.Entry.Message);
		}
	}
    public static ILogger CreateFileLogger(string path)
    {
        return new FileLogger(path);
    }



    event EntryWrittenEventHandler EntryWritten;

	void LogError(string message);
	void LogWarning(string message);
	void LogInfo(string message);
	void LogDebug(string message);
}
public class Logger : ILogger
{
	public event EntryWrittenEventHandler? EntryWritten;

	public void LogError(string message)
	{
		this.InvokeEntryWritten(new EntryWrittenEventArgs(message, LogEntryType.Error));
	}
	public void LogWarning(string message)
	{
		this.InvokeEntryWritten(new EntryWrittenEventArgs(message, LogEntryType.Warning));
	}
	public void LogInfo(string message)
	{
		this.InvokeEntryWritten(new EntryWrittenEventArgs(message, LogEntryType.Info));
	}
    public void LogDebug(string message)
    {
        this.InvokeEntryWritten(new EntryWrittenEventArgs(message, LogEntryType.Debug));
    }
    protected virtual void InvokeEntryWritten(EntryWrittenEventArgs e)
	{
		this.EntryWritten?.Invoke(this, e);
	}
}
class FileLogger : Logger, IDisposable
{
	private readonly int interval_ms;
	private readonly ThreadSafeList<LogEntry> entries;
	private readonly string path;
	private readonly Timer timer;
	//private readonly Task initTask;

    public FileLogger(string path, int interval_ms = 1_000)
    {
		this.entries = new ThreadSafeList<LogEntry>();
		//this.initTask = File.AppendAllLinesAsync(path, new[] { "--------------------------------------------------------------------------------" });
        this.path = path;
		this.interval_ms = interval_ms;
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        this.timer = new Timer(@this => ((FileLogger)@this!).Flush(), this, this.interval_ms, this.interval_ms);
    }
    
	public void Flush()
	{
		var entries = this.entries.Clear();
		Flush(entries);
	}
	private void Flush(IEnumerable<LogEntry> entries)
	{
        var builder = new StringBuilder();
		foreach (var entry in entries)
		{
			this.Format(entry, builder);
		}

		//if (!initTask.IsCompleted)
		//{
			// this should have completed by now, but just in case we wait
			//initTask.Wait(1_000);
		//}

        File.AppendAllText(this.path, builder.ToString());
	}

    protected override void InvokeEntryWritten(EntryWrittenEventArgs e)
    {
        base.InvokeEntryWritten(e);
        entries.Add(e.Entry);
    }
    protected virtual void Format(LogEntry entry, StringBuilder builder)
	{
        var timestamp = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:fffff}";
        builder.Append(timestamp);
        builder.Append(" ");
        builder.Append(entry.Type.ToJustifiedString()); // Ends on ':'
        builder.Append(" ");
        builder.AppendLine(entry.Message);
	}

	private void OnProcessExit(object? sender, EventArgs e)
	{
		this.Flush();
	}
    public void Dispose()
    {
		this.timer.Dispose();
		AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
    }
}