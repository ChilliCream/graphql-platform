using System.Text.Json;
using HotChocolate.Transport.Sockets.Client;

namespace HotChocolate.Fusion.Clients;

public sealed class GraphQLResponse : IDisposable
{
    private readonly IDisposable? _resource;

    internal GraphQLResponse(OperationResult result)
    {
        _resource = result;
        Data = result.Data ?? new();
        Errors = result.Errors ?? new();
        Extensions = result.Extensions ?? new();
    }

    public GraphQLResponse(JsonDocument document)
    {
        _resource = document;

        if (document.RootElement.TryGetProperty(ResponseProperties.Data, out var value))
        {
            Data = value;
        }

        if (document.RootElement.TryGetProperty(ResponseProperties.Errors, out value))
        {
            Errors = value;
        }

        if (document.RootElement.TryGetProperty(ResponseProperties.Extensions, out value))
        {
            Extensions = value;
        }
    }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public void Dispose()
        => _resource?.Dispose();
}
