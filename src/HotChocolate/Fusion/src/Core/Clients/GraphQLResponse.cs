using System.Text.Json;

namespace HotChocolate.Fusion.Clients;

public sealed class GraphQLResponse : IDisposable
{
    private readonly JsonDocument? _document;

    public GraphQLResponse(JsonDocument? document)
    {
        _document = document;

        if (_document is not null)
        {
            if (_document.RootElement.TryGetProperty(ResponseProperties.Data, out var value))
            {
                Data = value;
            }

            if (_document.RootElement.TryGetProperty(ResponseProperties.Errors, out value))
            {
                Errors = value;
            }

            if (_document.RootElement.TryGetProperty(ResponseProperties.Extensions, out value))
            {
                Extensions = value;
            }
        }
    }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public void Dispose()
    {
        _document?.Dispose();
    }
}
