namespace HotChocolate.Fusion.Logging.Contracts;

public interface ILoggingSession
{
    int InfoCount { get; }

    int WarningCount { get; }

    int ErrorCount { get; }

    void Write(LogEntry entry);
}
