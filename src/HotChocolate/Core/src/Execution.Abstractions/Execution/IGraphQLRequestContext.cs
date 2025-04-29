using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the GraphQL request context.
/// </summary>
public interface IGraphQLRequestContext : IFeatureProvider
{
    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the GraphQL request service provider.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the GraphQL request definition.
    /// </summary>
    IOperationRequest Request { get; }

    /// <summary>
    /// Gets the request cancellation token.
    /// </summary>
    CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets the
    /// </summary>
    IExecutionResult? Result { get; set; }
}
