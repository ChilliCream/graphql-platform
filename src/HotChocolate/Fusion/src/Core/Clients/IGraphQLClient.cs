namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a client for making GraphQL requests to a subgraph.
/// </summary>
public interface IGraphQLClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the subgraph that this client is connected to.
    /// </summary>
    string SubgraphName { get; }

    /// <summary>
    /// Executes a single GraphQL request asynchronously and returns the response.
    /// </summary>
    /// <param name="request">
    /// The GraphQL request to execute.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, which returns the GraphQL response.
    /// </returns>
    Task<GraphQLResponse> ExecuteAsync(
        SubgraphGraphQLRequest request,
        CancellationToken cancellationToken);
}
