using System.Text.Json;
using HotChocolate.Execution.Serialization;

namespace HotChocolate.AspNetCore.Serialization;

public class DefaultWebSocketPayloadFormatter(WebSocketPayloadFormatterOptions options)
    : IWebSocketPayloadFormatter
{
    private readonly JsonResultFormatter _jsonFormatter = new(options.Json);

    public void Format(IOperationResult result, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.Format(result, jsonWriter);
    }

    public void Format(IError error, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.FormatError(error, jsonWriter);
    }

    public void Format(IReadOnlyList<IError> errors, Utf8JsonWriter jsonWriter)
    {
        _jsonFormatter.FormatErrors(errors, jsonWriter);
    }
}
