namespace HotChocolate.Fusion.Logging.Contracts;

/// <summary>
/// Defines an interface for logging composition information, warnings, and errors.
/// </summary>
public interface ICompositionLog : IEnumerable<LogEntry>
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
    /// Writes the specified <see cref="LogEntry"/> to the log.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to write.</param>
    void Write(LogEntry entry);
}
