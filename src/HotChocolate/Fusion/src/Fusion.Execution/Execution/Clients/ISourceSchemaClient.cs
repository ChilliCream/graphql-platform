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
    /// Gets the capabilities of this client
    /// </summary>
    SourceSchemaClientCapabilities Capabilities { get; }

    /// <summary>
    /// Executes a single GraphQL operation against the source schema and streams the
    /// results back as they arrive.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An async stream of <see cref="SourceSchemaResult"/> items produced by the source schema.
    /// </returns>
    IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes multiple GraphQL operations as a single batched transport request and
    /// streams results back as they arrive. Each result is tagged with its request index
    /// so the caller can route it to the correct operation.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="requests">The requests to include in the batch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An async stream of <see cref="SourceSchemaBatchResult"/> where each item contains
    /// the request index and the corresponding <see cref="SourceSchemaResult"/>.
    /// </returns>
    IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to a GraphQL subscription operation against the source schema and streams the
    /// events back as they arrive.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="request">The subscription request to execute.</param>
    /// <param name="cancellationToken">A token to cancel the subscription.</param>
    /// <returns>
    /// An async stream of <see cref="SourceSchemaResult"/> items produced by the subscription.
    /// </returns>
    IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
