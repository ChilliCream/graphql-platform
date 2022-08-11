using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

public sealed class Response : IDisposable
{
    private readonly JsonDocument? _document;

    public Response(JsonDocument? document)
    {
        _document = document;

        if (_document is not null && _document.RootElement.TryGetProperty("data", out var data))
        {
            Data = data;
        }
    }

    public JsonElement Data { get; }

    public void Dispose()
    {
        _document?.Dispose();
    }
}
