using HotChocolate.Buffers;
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
    private MemoryArena? _memory;

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
    public override IDictionary<string, object?> ContextData { get; } = new RequestContextData();

    /// <summary>
    /// Gets the memory arena that backs request-scoped allocations, or <c>null</c> when none is attached.
    /// </summary>
    internal MemoryArena? Memory => _memory;

    /// <summary>
    /// Attaches a memory arena to this request. The arena is owned by the request until it is
    /// detached on success or disposed when the context is reset.
    /// </summary>
    internal void AttachMemory(MemoryArena memory)
    {
        if (_memory is not null)
        {
            throw new InvalidOperationException("A memory arena is already attached to this request.");
        }

        _memory = memory;
    }

    /// <summary>
    /// Detaches the memory arena so its ownership can be transferred to the execution result.
    /// </summary>
    internal MemoryArena DetachMemory()
        => Interlocked.Exchange(ref _memory, null)
            ?? throw new InvalidOperationException("No memory arena is attached to this request.");

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
        // dispose the arena if it was never detached (error or zero-event paths).
        Interlocked.Exchange(ref _memory, null)?.Dispose();
        _features.Reset();
        ContextData.Clear();
    }
}
