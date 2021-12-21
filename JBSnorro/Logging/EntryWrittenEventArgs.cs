namespace JBSnorro.Logging
{
	public delegate void EntryWrittenEventHandler(object sender, EntryWrittenEventArgs e);
	public class EntryWrittenEventArgs
	{
		public LogEntry Entry { get; }

		public EntryWrittenEventArgs(LogEntry entry)
			=> Entry = entry;

		public EntryWrittenEventArgs(string message, LogEntryType type)
			: this(new LogEntry(message, type))
		{
		}
	}
}
