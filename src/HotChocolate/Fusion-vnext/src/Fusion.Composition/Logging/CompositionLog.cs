using System.Collections;
using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion.Logging;

public sealed class CompositionLog : ICompositionLog
{
    public bool HasErrors { get; private set; }

    public bool IsEmpty => _entries.Count == 0;

    private readonly List<LogEntry> _entries = [];

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Severity == LogSeverity.Error)
        {
            HasErrors = true;
        }

        _entries.Add(entry);
    }

    public IEnumerator<LogEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
