namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Defines the kind of a log entry.
/// </summary>
public enum LogEntryKind
{
    /// <summary>
    /// The entry contains informational message.
    /// </summary>
    Info,

    /// <summary>
    /// The entry contains a warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// The entry contains an error message.
    /// </summary>
    Error
}
