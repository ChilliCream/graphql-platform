using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents an entry in a composition log that describes a problem or issue encountered
/// during the composition process.
/// </summary>
public sealed record LogEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> record with the specified values.
    /// </summary>
    public LogEntry(
        string message,
        string? code = null,
        LogEntryKind kind = LogEntryKind.Error,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Schema? schema = null,
        Exception? exception = null,
        object? extension = null)
    {
        Message = message;
        Code = code;
        Kind = kind;
        Coordinate = coordinate;
        Member = member;
        Schema = schema;
        Exception = exception;
        Extension = extension;
    }

    /// <summary>
    /// Gets the message associated with this log entry.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional code associated with this log entry.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the kind of log entry.
    /// </summary>
    public LogEntryKind Kind { get; }

    /// <summary>
    /// Gets the schema coordinate associated with this log entry.
    /// </summary>
    public SchemaCoordinate? Coordinate { get; }

    /// <summary>
    /// Gets the type system member associated with this log entry.
    /// </summary>
    public ITypeSystemMember? Member { get; }

    /// <summary>
    /// Gets the schema associated with this log entry.
    /// </summary>
    public Schema? Schema { get; }

    /// <summary>
    /// Gets the exception associated with this log entry.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the extension object associated with this log entry.
    /// </summary>
    public object? Extension { get; }

    /// <summary>
    /// Deconstructs the <see cref="LogEntry"/> record into its individual values.
    /// </summary>
    public void Deconstruct(
        out string message,
        out string? code,
        out LogEntryKind kind,
        out SchemaCoordinate? coordinate,
        out ITypeSystemMember? member,
        out Schema? schema,
        out Exception? exception,
        out object? extension)
    {
        message = Message;
        code = Code;
        kind = Kind;
        coordinate = Coordinate;
        member = Member;
        schema = Schema;
        exception = Exception;
        extension = Extension;
    }
}
