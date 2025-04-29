using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the GraphQL request context.
/// </summary>
public abstract class GraphQLRequestContext : IFeatureProvider
{
    protected GraphQLRequestContext(
        ISchemaDefinition schema,
        IOperationRequest request,
        IServiceProvider requestServices)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        RequestServices = requestServices ?? throw new ArgumentNullException(nameof(requestServices));
    }

    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the GraphQL request definition.
    /// </summary>
    public IOperationRequest Request { get; }

    /// <summary>
    /// Gets the GraphQL request service provider.
    /// </summary>
    public IServiceProvider RequestServices { get; set; }

    /// <summary>
    /// Gets the request cancellation token.
    /// </summary>
    public CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets GraphQL execution result.
    /// </summary>
    public IExecutionResult? Result { get; set; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();
}

public sealed record OperationDocumentInfo
{
    /// <summary>
    /// Gets or sets the parsed query document.
    /// </summary>
    public required DocumentNode Document { get; init; }

    /// <summary>
    /// Gets or sets the document hash.
    /// </summary>
    public required string DocumentHash { get; init; }

    /// <summary>
    /// Defines that the document was retrieved from the cache.
    /// </summary>
    public required bool IsCachedDocument { get; init; }

    /// <summary>
    /// Defines that the document was retrieved from a query storage.
    /// </summary>
    public required bool IsPersistedDocument { get; init; }
}

public static class GraphQLRequestContextExtensions
{
    public static OperationDocumentInfo? GetOperationDocumentInfo(
        this GraphQLRequestContext context)
        => context.Features.Get<OperationDocumentInfo>();

    public static void SetOperationDocumentInfo(
        this GraphQLRequestContext context,
        OperationDocumentInfo metadata)
        => context.Features.Set(metadata);
}
