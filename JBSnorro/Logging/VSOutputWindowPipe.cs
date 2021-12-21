using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Logging
{
	/// <summary>
	/// Pipes the output of the logger to a simple list of log entries.
	/// </summary>
	public class VSOutputWindowPipe : IDisposable
	{
		[DllImport("kernel32.dll")]
		private static extern void OutputDebugString(string lpOutputString);

		private readonly ILogger logger;
		public VSOutputWindowPipe(ILogger logger)
		{
			Contract.Requires(logger != null);

			this.logger = logger;
			logger.EntryWritten += entryWritten;
		}

		private void entryWritten(object sender, EntryWrittenEventArgs e)
		{
			OutputDebugString(e.Entry.Message);
		}

		public void Dispose()
		{
			this.logger.EntryWritten -= entryWritten;
		}
	}

}
