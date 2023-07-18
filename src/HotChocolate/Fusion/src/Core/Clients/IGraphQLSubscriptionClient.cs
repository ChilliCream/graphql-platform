namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a client for subscribing to a GraphQL subgraph.
/// </summary>
public interface IGraphQLSubscriptionClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the subgraph that this client is connected to.
    /// </summary>
    string SubgraphName { get; }

    /// <summary>
    /// Subscribes to a GraphQL subscription asynchronously and returns a stream of responses.
    /// </summary>
    /// <param name="request">
    /// The GraphQL subscription to subscribe to.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, which returns a stream of GraphQL responses.
    /// </returns>
    IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        SubgraphGraphQLRequest request,
        CancellationToken cancellationToken);
}
