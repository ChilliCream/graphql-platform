namespace HotChocolate.Logging.Contracts;

/// <summary>
/// Defines an interface for logging schema validation information, warnings, and errors.
/// </summary>
public interface IValidationLog : IEnumerable<LogEntry>
{
    /// <summary>
    /// Gets a value indicating whether the log contains errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Gets a value indicating whether the log is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Writes the specified entry to the log.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);

    /// <summary>
    /// Writes the specified entries to the log.
    /// </summary>
    /// <param name="entries">The log entries to write.</param>
    void Write(IEnumerable<LogEntry> entries);
}
