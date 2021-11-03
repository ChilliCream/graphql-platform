using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public class SubscribePayload
{
    public SubscribePayload(
        string? operationName,
        string? query,
        IDictionary<string, object?>? extensions,
        IDictionary<string, object?>? variables)
    {
        OperationName = operationName;
        Query = query;
        Extensions = extensions;
        Variables = variables;
    }

    public string? OperationName { get; }

    public string? Query { get; }

    public IDictionary<string, object?>? Extensions { get; }

    public IDictionary<string, object?>? Variables { get; }
}
