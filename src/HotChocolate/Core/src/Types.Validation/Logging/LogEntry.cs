using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Types;

namespace HotChocolate.Logging;

/// <summary>
/// Represents an entry in a schema validation log that describes an issue encountered during the
/// validation process.
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
    public ITypeSystemMember TypeSystemMember { get; set; } = null!;

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
    /// Returns a JSON string representation of the log entry.
    /// </summary>
    public override unsafe string ToString()
    {
        using var buffer = new PooledArrayWriter();
        using var writer = new Utf8JsonWriter(buffer, s_serializationOptions);

        writer.WriteStartObject();
        Serialize(writer);
        writer.WriteEndObject();

        writer.Flush();

        fixed (byte* b = PooledArrayWriterMarshal.GetUnderlyingBuffer(buffer))
        {
            return Encoding.UTF8.GetString(b, buffer.Length);
        }
    }

    private void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteString("message", Message);
        writer.WriteString("code", Code);
        writer.WriteString("severity", Severity.ToString());

        if (Coordinate is not null)
        {
            writer.WriteString("coordinate", Coordinate.ToString());
        }

        switch (TypeSystemMember)
        {
            case IDirectiveDefinition directiveDefinition:
                writer.WriteString("member", $"@{directiveDefinition.Name}");
                break;
            case INameProvider namedMember:
                writer.WriteString("member", namedMember.Name);
                break;
        }

        writer.WritePropertyName("extensions");
        writer.WriteStartObject();

        foreach (var item in Extensions.OrderBy(i => i.Key))
        {
            writer.WritePropertyName(item.Key);

            switch (item.Value)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case IFieldDefinition field:
                    writer.WriteStringValue(field.Name);
                    break;
                case ITypeDefinition type:
                    writer.WriteStringValue(type.Name);
                    break;
                default:
                    writer.WriteStringValue(item.Value.ToString());
                    break;
            }
        }

        writer.WriteEndObject();
    }

    private static readonly JsonWriterOptions s_serializationOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
