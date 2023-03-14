namespace JBSnorro.Logging
{
	public class LogEntry
	{
		public string Message { get; }
		public LogEntryType Type { get; }
		public DateTime Timestamp { get; }

		public LogEntry(string message, LogEntryType type)
		{
			this.Message = message;
			this.Type = type;
			this.Timestamp = DateTime.Now;
		}
	}
}
