using System.Collections.Concurrent;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the GraphQL request context.
/// </summary>
public abstract class RequestContext : IFeatureProvider
{
    protected RequestContext(
        ISchemaDefinition schema,
        IOperationRequest request,
        IServiceProvider requestServices)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestServices);

        Schema = schema;
        Request = request;
        RequestServices = requestServices;
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

    /// <summary>
    /// Gets the request context data which allows arbitrary data to be stored on the request context.
    /// </summary>
    public ConcurrentDictionary<string, object?> ContextData { get; } = [];
}

public sealed class OperationDocumentInfo : RequestContextFeature
{
    /// <summary>
    /// Gets or sets the parsed query document.
    /// </summary>
    public DocumentNode? Document { get; set; } = default!;

    /// <summary>
    /// Gets or sets a unique identifier for an operation document.
    /// </summary>
    public OperationDocumentId Id { get; set; }

    /// <summary>
    /// Gets or sets the document hash.
    /// </summary>
    public OperationDocumentHash Hash { get; set; }

    /// <summary>
    /// Defines that the document was retrieved from the cache.
    /// </summary>
    public bool IsCached { get; set; }

    /// <summary>
    /// Defines that the document was retrieved from a query storage.
    /// </summary>
    public bool IsPersisted { get; set; }

    /// <summary>
    /// Defines that the document has been validated.
    /// </summary>
    public bool IsValidated { get; set; }

    public override void Reset()
    {
        Document = null!;
        Id = default;
        Hash = default;
        IsCached = false;
        IsPersisted = false;
        IsValidated = false;
    }
}

public sealed class OperationInfo : RequestContextFeature
{
    public OperationDefinitionNode Operation { get; set; } = default!;
}

public static class GraphQLRequestContextExtensions
{
    public static OperationDocumentInfo GetOperationDocumentInfo(
        this RequestContext context)
        => context.Features.GetOrSet<OperationDocumentInfo>();

    public static void SetOperationDocument(
        this RequestContext context,
        DocumentNode document,
        OperationDocumentId? id,
        OperationDocumentHash? hash)
    {
        ArgumentNullException.ThrowIfNull(document);

        var documentInfo = context.GetOperationDocumentInfo();
        documentInfo.Document = document;
        documentInfo.Id = id ?? default;
        documentInfo.Hash = hash ?? default;
    }
}

public abstract class RequestContextFeature
{
    public virtual void Initialize(RequestContext context)
    {
    }

    public virtual void Reset()
    {
    }
}
