namespace JBSnorro.Logging
{
    public enum LogEntryType
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class LogEntryTypeExtensions
    {
        public static string ToJustifiedString(this LogEntryType logEntryType)
        {
            switch (logEntryType)
            {
                case LogEntryType.Debug:
                    return "DEBUG:";
                case LogEntryType.Info:
                    return "INFO: ";
                case LogEntryType.Warning:
                    return "WARN: ";
                case LogEntryType.Error:
                    return "ERROR:";
                default:
                    throw new ArgumentException($"Value ${(int)logEntryType} is not a defined {nameof(LogEntryType)}", nameof(logEntryType));
            }
        }
    }
}
