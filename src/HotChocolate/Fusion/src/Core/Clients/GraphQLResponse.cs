using System.Text.Json;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Clients;

public sealed class GraphQLResponse : IDisposable
{
    private readonly IDisposable? _resource;

    internal GraphQLResponse(Exception transportException)
    {
        TransportException = transportException;
    }

    internal GraphQLResponse(OperationResult result)
    {
        _resource = result;
        Data = result.Data;
        Errors = result.Errors;
        Extensions = result.Extensions;
    }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public Exception? TransportException { get; private set; }

    public void Dispose()
    {
        _resource?.Dispose();
    }
}
