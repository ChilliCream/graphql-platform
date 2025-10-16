using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Logging;

/// <summary>
/// Represents an entry in a composition log that describes an issue encountered during the
/// composition process.
/// </summary>
public sealed record LogEntry
{
    /// <summary>
    /// Gets the message associated with this log entry.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets the code associated with this log entry.
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Gets the severity of this log entry.
    /// </summary>
    public LogSeverity Severity { get; set; }

    /// <summary>
    /// Gets the schema coordinate associated with this log entry.
    /// </summary>
    public SchemaCoordinate? Coordinate { get; set; }

    /// <summary>
    /// Gets the type system member associated with this log entry.
    /// </summary>
    public ITypeSystemMember? TypeSystemMember { get; set; }

    /// <summary>
    /// Gets the schema associated with this log entry.
    /// </summary>
    public ISchemaDefinition? Schema { get; set; }

    /// <summary>
    /// Gets the extensions associated with this log entry.
    /// </summary>
    public ImmutableDictionary<string, object?> Extensions { get; set; }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, object?>.Empty;
#endif

    /// <summary>
    /// Returns a string representation of the log entry.
    /// </summary>
    public override string ToString() => Message;
}
