using System;
using System.Threading;

namespace JBSnorro.Logging
{
	/// <summary>
	/// Pipes the output of the logger to the console.
	/// </summary>
	public class LoggerConsolePipe : IDisposable
	{
		private readonly bool prependThreadId;
		private readonly bool prependTimeStamp;
		private readonly ILogger log;
		public LoggerConsolePipe(ILogger log, bool prependTimeStamp = true, bool prependThreadId = false)
		{
			this.log = log;
			this.prependTimeStamp = prependTimeStamp;
			this.prependThreadId = prependThreadId;
			log.EntryWritten += entryWritten;
		}
		public static LoggerConsolePipe CreateDefault(out ILogger logger)
		{
			logger = new Logger();
			return new LoggerConsolePipe(logger);
		}

		private void entryWritten(object sender, EntryWrittenEventArgs e)
		{
			string threadId = prependThreadId ? $"({Thread.CurrentThread.ManagedThreadId})" : "";
			string timestamp = prependTimeStamp ? $"{DateTime.Now:hh:mm:ss.fff}: " : "";
			Console.WriteLine($"{timestamp}{e.Entry.Type}{threadId}: {e.Entry.Message}");
		}

		public void Dispose()
		{
			this.log.EntryWritten -= entryWritten;
		}
	}
}
