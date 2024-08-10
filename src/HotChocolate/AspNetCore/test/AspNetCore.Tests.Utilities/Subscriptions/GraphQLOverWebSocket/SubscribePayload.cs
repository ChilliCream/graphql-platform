namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;

public sealed class SubscribePayload
{
    public SubscribePayload(
        string? query,
        string? queryId = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        if (query is null && queryId is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        QueryId = queryId;
        Query = query;
        OperationName = operationName;
        Variables = variables;
        Extensions = extensions;
    }

    public string? QueryId { get; }

    public string? Query { get; }

    public string? OperationName { get; }

    public IReadOnlyDictionary<string, object?>? Variables { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }
}
