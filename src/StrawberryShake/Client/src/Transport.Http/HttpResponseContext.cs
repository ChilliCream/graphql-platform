using System.Text.Json;
using HotChocolate.Transport.Http;

namespace StrawberryShake.Transport.Http;

public readonly struct HttpResponseContext
{
    public HttpResponseContext(
        GraphQLHttpResponse response,
        JsonDocument? body,
        Exception? exception,
        bool isPatch = false,
        bool hasNext = false,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        Response = response;
        Body = body;
        Exception = exception;
        IsPatch = isPatch;
        HasNext = hasNext;
        Extensions = extensions;
        ContextData = contextData;
    }

    public GraphQLHttpResponse Response { get; }
    public JsonDocument? Body { get; }
    public Exception? Exception { get; }
    public bool IsPatch { get; }
    public bool HasNext { get; }
    public IReadOnlyDictionary<string, object?>? Extensions { get; }
    public IReadOnlyDictionary<string, object?>? ContextData { get; }
}
