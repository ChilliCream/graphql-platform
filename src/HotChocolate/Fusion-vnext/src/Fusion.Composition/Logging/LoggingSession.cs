using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion.Logging;

public sealed class LoggingSession(ICompositionLog compositionLog) : ILoggingSession
{
    public int InfoCount { get; private set; }

    public int WarningCount { get; private set; }

    public int ErrorCount { get; private set; }

    public void Write(LogEntry entry)
    {
        switch (entry.Severity)
        {
            case LogSeverity.Info:
                InfoCount++;
                break;

            case LogSeverity.Warning:
                WarningCount++;
                break;

            case LogSeverity.Error:
                ErrorCount++;
                break;

            default:
                throw new InvalidOperationException();
        }

        compositionLog.Write(entry);
    }
}
