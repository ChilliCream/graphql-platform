using System.Text.Json;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This represents the default implementation for the <see cref="IWebSocketPayloadFormatter" />.
/// </summary>
public sealed class DefaultWebSocketPayloadFormatter(WebSocketPayloadFormatterOptions options = default)
    : IWebSocketPayloadFormatter
{
    private readonly JsonSerializerOptions _serializerOptions = options.Json.CreateSerializerOptions();
    private readonly JsonResultFormatter _internalFormatter = new(options.Json);
    private readonly JsonNullIgnoreCondition _nullIgnoreCondition = options.Json.NullIgnoreCondition;

    /// <inheritdoc />
    public void Format(OperationResult result, JsonWriter writer)
        =>  _internalFormatter.Format(result, writer);

    /// <inheritdoc />
    public void Format(IError error, JsonWriter writer)
        => WriteError(writer, error, _serializerOptions, _nullIgnoreCondition);

    /// <inheritdoc />
    public void Format(IReadOnlyList<IError> errors, JsonWriter writer)
    {
        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            WriteError(writer, errors[i], _serializerOptions, _nullIgnoreCondition);
        }

        writer.WriteEndArray();
    }

    /// <inheritdoc />
    public void Format(IReadOnlyDictionary<string, object?> extensions, JsonWriter writer)
        => WriteDictionary(writer, extensions, _serializerOptions, _nullIgnoreCondition);
}
