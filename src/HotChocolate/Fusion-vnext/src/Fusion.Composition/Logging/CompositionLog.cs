using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion.Logging;

public sealed class CompositionLog : ICompositionLog
{
    public IList<LogEntry> Entries { get; } = [];

    public int EntryCount => Entries.Count;

    public bool IsEmpty => Entries.Count == 0;

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entries.Add(entry);
    }

    public ILoggingSession CreateSession()
    {
        return new LoggingSession(this);
    }
}
