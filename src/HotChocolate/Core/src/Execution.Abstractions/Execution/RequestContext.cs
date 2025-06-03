using System.Collections.Concurrent;
using System.Collections.Immutable;
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
        IServiceProvider requestServices,
        ulong executorVersion)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestServices);

        Schema = schema;
        Request = request;
        RequestServices = requestServices;
        ExecutorVersion = executorVersion;
    }

    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the request executor version.
    /// </summary>
    public ulong ExecutorVersion { get; }

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
    /// Gets or sets the request index.
    /// </summary>
    public int RequestIndex { get; set; } = -1;

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
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Gets the request context data which allows arbitrary data to be stored on the request context.
    /// </summary>
    public ConcurrentDictionary<string, object?> ContextData { get; } = [];
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
