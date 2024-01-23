using System.Collections;

namespace HotChocolate.Fusion.Composition;

internal sealed class DefaultCompositionLog : ICompositionLog, IEnumerable<LogEntry>
{
    private readonly ICompositionLog? _innerLog;

    public DefaultCompositionLog(ICompositionLog? innerLog = null)
    {
        _innerLog = innerLog;
    }

    private readonly List<LogEntry> _entries = [];

    public bool HasErrors { get; private set; }

    public void Write(LogEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry.Severity is LogSeverity.Error)
        {
            HasErrors = true;
        }

        _entries.Add(entry);

        _innerLog?.Write(entry);
    }

    public IEnumerator<LogEntry> GetEnumerator()
        => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
