using System.Collections.Concurrent;
using HotChocolate.Features;

namespace HotChocolate.Execution;

internal sealed class DefaultRequestContext : RequestContext
{
    private readonly PooledFeatureCollection _features;

    private ISchemaDefinition _schema = null!;
    private ulong _executorVersion;
    private IOperationRequest _request = null!;
    private int _requestIndex;

    public DefaultRequestContext()
    {
        _features = new PooledFeatureCollection(this);
        OperationDocumentInfo = _features.GetOrSet<OperationDocumentInfo>();
    }

    public override ISchemaDefinition Schema => _schema;

    public override ulong ExecutorVersion => _executorVersion;

    public override IOperationRequest Request => _request;

    public override int RequestIndex => _requestIndex;

    public override IServiceProvider RequestServices { get; set; } = null!;

    public override OperationDocumentInfo OperationDocumentInfo { get; }

    public override IFeatureCollection Features => _features;

    public override IDictionary<string, object?> ContextData { get; } = new ConcurrentDictionary<string, object?>();


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

        _features.Initialize(request.Features);

        if (request.ContextData is not null)
        {
            foreach (var (key, value) in request.ContextData)
            {
                ContextData.Add(key, value);
            }
        }
    }

    public void Reset()
    {
        _schema = null!;
        _executorVersion = 0;
        _request = null!;
        RequestServices = null!;
        RequestAborted = CancellationToken.None;
        _features.Reset();
        ContextData.Clear();
    }
}
