namespace HotChocolate.Language;

/// <summary>
/// Represents the parsed GraphQL request JSON object.
/// </summary>
public sealed class GraphQLRequest
{
    public GraphQLRequest(
        DocumentNode? query,
        string? queryId = null,
        string? queryHash = null,
        string? operationName = null,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        if (query is null && queryId is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        QueryId = queryId;
        QueryHash = queryHash;
        Query = query;
        OperationName = operationName;
        Variables = variables;
        Extensions = extensions;
    }

    public string? QueryId { get; }

    public string? QueryHash { get; }

    public DocumentNode? Query { get; }

    public string? OperationName { get; }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? Variables { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }
}
