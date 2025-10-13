using System.Collections;
using HotChocolate.Logging.Contracts;

namespace HotChocolate.Logging;

/// <summary>
/// Represents a log for schema validation.
/// </summary>
public sealed class ValidationLog : IValidationLog
{
    /// <inheritdoc />
    public bool HasErrors { get; private set; }

    /// <inheritdoc />
    public bool IsEmpty => _entries.Count == 0;

    private readonly List<LogEntry> _entries = [];

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Severity == LogSeverity.Error)
        {
            HasErrors = true;
        }

        _entries.Add(entry);
    }

    /// <inheritdoc />
    public void Write(IEnumerable<LogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            if (entry.Severity == LogSeverity.Error)
            {
                HasErrors = true;
            }

            _entries.Add(entry);
        }
    }

    /// <inheritdoc />
    public IEnumerator<LogEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
