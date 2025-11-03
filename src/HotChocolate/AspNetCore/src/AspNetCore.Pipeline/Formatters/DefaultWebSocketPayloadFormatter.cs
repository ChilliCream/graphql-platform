using System.Text.Json;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This represents the default implementation for the <see cref="IWebSocketPayloadFormatter" />.
/// </summary>
public class DefaultWebSocketPayloadFormatter(WebSocketPayloadFormatterOptions options = default)
    : IWebSocketPayloadFormatter
{
    private readonly JsonResultFormatter _jsonFormatter = new(options.Json);

    /// <inheritdoc />
    public void Format(IOperationResult result, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.Format(result, jsonWriter);
    }

    /// <inheritdoc />
    public void Format(IError error, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.FormatError(error, jsonWriter);
    }

    /// <inheritdoc />
    public void Format(IReadOnlyList<IError> errors, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.FormatErrors(errors, jsonWriter);
    }

    /// <inheritdoc />
    public void Format(IReadOnlyDictionary<string, object?> extensions, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.FormatDictionary(extensions, jsonWriter);
    }
}
