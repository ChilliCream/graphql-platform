using System.Collections;
using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion.Logging;

/// <summary>
/// A composition log that forwards every entry to a parent log while exposing only the entries
/// written through this scope. <see cref="HasErrors"/> reflects only those entries.
/// </summary>
internal sealed class ScopedCompositionLog(ICompositionLog parent) : ICompositionLog
{
    private readonly List<LogEntry> _entries = [];

    public bool HasErrors { get; private set; }

    public bool IsEmpty => _entries.Count == 0;

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Severity == LogSeverity.Error)
        {
            HasErrors = true;
        }

        _entries.Add(entry);
        parent.Write(entry);
    }

    public IEnumerator<LogEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
