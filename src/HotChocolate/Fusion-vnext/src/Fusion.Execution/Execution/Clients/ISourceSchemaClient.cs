using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents a transport-level client for a single source schema.
/// Implementations handle the wire protocol (e.g. HTTP, WebSocket) for
/// sending GraphQL operations to a downstream service.
/// </summary>
public interface ISourceSchemaClient : IAsyncDisposable
{
    /// <summary>
    /// Executes a single GraphQL operation against the source schema.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the source schema.</returns>
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes multiple GraphQL operations as a single batched transport request.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="requests">The requests to include in the batch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A dictionary mapping each request's <see cref="SourceSchemaClientRequest.Node"/> ID
    /// to its corresponding response.
    /// </returns>
    ValueTask<ImmutableArray<SourceSchemaClientResponse>> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken);
}
