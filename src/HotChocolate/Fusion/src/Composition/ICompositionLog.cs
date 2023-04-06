namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Defines an interface for logging composition information and errors.
/// </summary>
public interface ICompositionLog
{
    /// <summary>
    /// Gets a value indicating whether the log has any errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Writes the specified <see cref="LogEntry"/> to the log.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to write.</param>
    void Write(LogEntry entry);
}
