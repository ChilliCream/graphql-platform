using System.Buffers;
using System.Text.Json;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This represents the default implementation for the <see cref="IWebSocketPayloadFormatter" />.
/// </summary>
public class DefaultWebSocketPayloadFormatter(WebSocketPayloadFormatterOptions options = default)
    : IWebSocketPayloadFormatter
{
    private readonly JsonWriterOptions _writerOptions = options.Json.CreateWriterOptions();
    private readonly JsonSerializerOptions _serializerOptions = options.Json.CreateSerializerOptions();
    private readonly JsonNullIgnoreCondition _nullIgnoreCondition = options.Json.NullIgnoreCondition;

    /// <inheritdoc />
    public void Format(OperationResult result, IBufferWriter<byte> bufferWriter)
    {
        var writer = new JsonWriter(bufferWriter, _writerOptions);
        WriteValue(writer, result, _serializerOptions, _nullIgnoreCondition);
    }

    /// <inheritdoc />
    public void Format(IError error, IBufferWriter<byte> bufferWriter)
    {
        var writer = new JsonWriter(bufferWriter, _writerOptions);
        WriteError(writer, error, _serializerOptions, _nullIgnoreCondition);
    }

    /// <inheritdoc />
    public void Format(IReadOnlyList<IError> errors, IBufferWriter<byte> bufferWriter)
    {
        var writer = new JsonWriter(bufferWriter, _writerOptions);
        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            WriteError(writer, errors[i], _serializerOptions, _nullIgnoreCondition);
        }

        writer.WriteEndArray();
    }

    /// <inheritdoc />
    public void Format(IReadOnlyDictionary<string, object?> extensions, IBufferWriter<byte> bufferWriter)
    {
        var writer = new JsonWriter(bufferWriter, _writerOptions);
        WriteDictionary(writer, extensions, _serializerOptions, _nullIgnoreCondition);
    }
}
