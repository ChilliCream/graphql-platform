using System.Collections;

namespace HotChocolate.Fusion.Composition;

internal sealed class DefaultCompositionLog : ICompositionLog, IEnumerable<LogEntry>
{
    private readonly List<LogEntry> _entries = new();

    public bool HasErrors { get; private set; }

    public void Write(LogEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry.Kind is LogEntryKind.Error)
        {
            HasErrors = true;
        }

        _entries.Add(entry);
    }

    public IEnumerator<LogEntry> GetEnumerator()
        => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
