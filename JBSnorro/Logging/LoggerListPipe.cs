using System;
using System.Collections.Generic;

namespace JBSnorro.Logging
{
	/// <summary>
	/// Pipes the output of the logger to a simple list of log entries.
	/// </summary>
	public class LoggerListPipe : IDisposable
	{
		private readonly object _lock = new object();
		private readonly IList<LogEntry> output;
		private readonly ILogger log;
		public LoggerListPipe(ILogger log, IList<LogEntry> output)
		{
			this.log = log;
			this.output = output;
			log.EntryWritten += entryWritten;
		}

		private void entryWritten(object sender, EntryWrittenEventArgs e)
		{
			lock (_lock)
			{
				this.output.Add(e.Entry);
			}
		}

		public void Dispose()
		{
			log.EntryWritten -= entryWritten;
		}
	}

}
