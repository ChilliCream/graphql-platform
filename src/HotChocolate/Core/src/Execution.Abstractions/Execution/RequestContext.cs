using System.Collections.Immutable;
using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the GraphQL request context.
/// </summary>
public abstract class RequestContext : IFeatureProvider
{
    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    public abstract ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the request executor version.
    /// </summary>
    public abstract ulong ExecutorVersion { get; }

    /// <summary>
    /// Gets the GraphQL request definition.
    /// </summary>
    public abstract IOperationRequest Request { get; }

    /// <summary>
    /// Gets the GraphQL request service provider.
    /// </summary>
    public abstract IServiceProvider RequestServices { get; set; }

    /// <summary>
    /// Gets information about the GraphQL operation document of the GraphQL <see cref="Request"/>.
    /// </summary>
    public abstract OperationDocumentInfo OperationDocumentInfo { get; }

    /// <summary>
    /// Gets the request cancellation token.
    /// </summary>
    public CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets the request index.
    /// </summary>
    public abstract int RequestIndex { get; }

    /// <summary>
    /// Gets or sets the variable values set.
    /// </summary>
    public ImmutableArray<IVariableValueCollection> VariableValues { get; set; } = [];

    /// <summary>
    /// Gets or sets GraphQL execution result.
    /// </summary>
    public IExecutionResult? Result { get; set; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the request context data which allows arbitrary data to be stored on the request context.
    /// </summary>
    public abstract IDictionary<string, object?> ContextData { get; }
}
