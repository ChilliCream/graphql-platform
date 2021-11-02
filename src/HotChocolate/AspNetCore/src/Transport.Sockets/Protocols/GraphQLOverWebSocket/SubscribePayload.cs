using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public class SubscribePayload
{
    public SubscribePayload(
        string? operationName,
        DocumentNode? query,
        IDictionary<string, object?>? extensions,
        IDictionary<string, object?>? variables)
    {
        OperationName = operationName;
        Query = query;
        Extensions = extensions;
        Variables = variables;
    }

    public string? OperationName { get; }

    public DocumentNode? Query { get; }

    public IDictionary<string, object?>? Extensions { get; }

    public IDictionary<string, object?>? Variables { get; }
}
