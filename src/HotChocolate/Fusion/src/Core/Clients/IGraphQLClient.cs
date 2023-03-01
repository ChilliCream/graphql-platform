namespace HotChocolate.Fusion.Clients;

public interface IGraphQLClient
{
    string SubGraphName { get; }

    Task<GraphQLResponse> ExecuteAsync(
        GraphQLRequest request,
        CancellationToken cancellationToken);

    Task<IAsyncEnumerable<GraphQLResponse>> ExecuteBatchAsync(
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken);

    Task<IAsyncEnumerable<GraphQLResponse>> SubscribeAsync(
        GraphQLRequest graphQLRequests,
        CancellationToken cancellationToken);
}
