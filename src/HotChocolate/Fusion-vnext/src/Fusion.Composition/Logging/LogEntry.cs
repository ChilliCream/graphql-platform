using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Logging;

/// <summary>
/// Represents an entry in a composition log that describes an issue encountered during the
/// composition process.
/// </summary>
public sealed record LogEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> record with the specified values.
    /// </summary>
    public LogEntry(
        string message,
        string code,
        LogSeverity severity = LogSeverity.Error,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMemberDefinition? member = null,
        MutableSchemaDefinition? schema = null,
        object? extension = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(code);

        Message = message;
        Code = code;
        Severity = severity;
        Coordinate = coordinate;
        Member = member;
        Schema = schema;
        Extension = extension;
    }

    /// <summary>
    /// Gets the message associated with this log entry.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the code associated with this log entry.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the severity of this log entry.
    /// </summary>
    public LogSeverity Severity { get; }

    /// <summary>
    /// Gets the schema coordinate associated with this log entry.
    /// </summary>
    public SchemaCoordinate? Coordinate { get; }

    /// <summary>
    /// Gets the type system member associated with this log entry.
    /// </summary>
    public ITypeSystemMemberDefinition? Member { get; }

    /// <summary>
    /// Gets the schema associated with this log entry.
    /// </summary>
    public MutableSchemaDefinition? Schema { get; }

    /// <summary>
    /// Gets the extension object associated with this log entry.
    /// </summary>
    public object? Extension { get; }

    public override string ToString() => Message;
}
