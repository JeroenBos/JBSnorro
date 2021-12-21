namespace JBSnorro.Logging
{
	public interface ILogger
	{
		event EntryWrittenEventHandler EntryWritten;

		void LogError(string message);
		void LogWarning(string message);
		void LogInfo(string message);
	}
	public class Logger : ILogger
	{
		public event EntryWrittenEventHandler EntryWritten;

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

		protected virtual void InvokeEntryWritten(EntryWrittenEventArgs entry)
		{
			this.EntryWritten?.Invoke(this, entry);
		}
	}
}
