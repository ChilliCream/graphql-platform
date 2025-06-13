using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a pooled request context.
/// </summary>
public sealed class PooledRequestContext : RequestContext
{
    private readonly PooledFeatureCollection _features;

    private ISchemaDefinition _schema = null!;
    private ulong _executorVersion;
    private IOperationRequest _request = null!;
    private int _requestIndex;

    /// <summary>
    /// Initializes a new instance of <see cref="PooledRequestContext"/>.
    /// </summary>
    public PooledRequestContext()
    {
        _features = new PooledFeatureCollection(this);
        OperationDocumentInfo = _features.GetOrSet<OperationDocumentInfo>();
    }

    /// <inheritdoc />
    public override ISchemaDefinition Schema => _schema;

    /// <inheritdoc />
    public override ulong ExecutorVersion => _executorVersion;

    /// <inheritdoc />
    public override IOperationRequest Request => _request;

    /// <inheritdoc />
    public override int RequestIndex => _requestIndex;

    /// <inheritdoc />
    public override IServiceProvider RequestServices { get; set; } = null!;

    /// <inheritdoc />
    public override OperationDocumentInfo OperationDocumentInfo { get; }

    /// <inheritdoc />
    public override IFeatureCollection Features => _features;

    /// <inheritdoc />
    public override IDictionary<string, object?> ContextData { get; } = new ConcurrentDictionary<string, object?>();

    /// <summary>
    /// Initializes the request context after renting it from the pool.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <param name="executorVersion">The executor version.</param>
    /// <param name="request">The request.</param>
    /// <param name="requestIndex">The request index.</param>
    /// <param name="requestServices">The request services.</param>
    /// <param name="requestAborted">The request aborted.</param>
    public void Initialize(
        ISchemaDefinition schema,
        ulong executorVersion,
        IOperationRequest request,
        int requestIndex,
        IServiceProvider requestServices,
        CancellationToken requestAborted)
    {
        _schema = schema;
        _executorVersion = executorVersion;
        _request = request;
        _requestIndex = requestIndex;
        RequestServices = requestServices;
        RequestAborted = requestAborted;

        if (!request.DocumentId.IsEmpty)
        {
            OperationDocumentInfo.Id = request.DocumentId;
        }

        if (!request.DocumentHash.IsEmpty)
        {
            OperationDocumentInfo.Hash = request.DocumentHash;

            if (OperationDocumentInfo.Id.IsEmpty)
            {
                OperationDocumentInfo.Id = new OperationDocumentId(OperationDocumentInfo.Hash.Value);
            }
        }

        _features.Initialize(request.Features);

        if (request.ContextData is not null)
        {
            foreach (var (key, value) in request.ContextData)
            {
                ContextData.Add(key, value);
            }
        }
    }

    /// <summary>
    /// Resets the request context to its initial state.
    /// </summary>
    public void Reset()
    {
        _schema = null!;
        _executorVersion = 0;
        _request = null!;
        _requestIndex = -1;
        RequestServices = null!;
        RequestAborted = CancellationToken.None;
        VariableValues = [];
        Result = null;
        _features.Reset();
        ContextData.Clear();
    }
}
