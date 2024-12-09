using System.Collections;
using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion.Logging;

public sealed class CompositionLog : ICompositionLog, IEnumerable<LogEntry>
{
    public bool IsEmpty => _entries.Count == 0;

    private readonly List<LogEntry> _entries = [];

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Add(entry);
    }

    public ILoggingSession CreateSession()
    {
        return new LoggingSession(this);
    }

    public IEnumerator<LogEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
