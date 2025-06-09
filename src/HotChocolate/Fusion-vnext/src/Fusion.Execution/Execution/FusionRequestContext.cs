using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a Fusion GraphQL request context.
/// </summary>
public sealed class FusionRequestContext : RequestContext
{
    private readonly PooledFeatureCollection _features;

    private ISchemaDefinition _schema = null!;
    private ulong _executorVersion;
    private IOperationRequest _request = null!;
    private int _requestIndex;

    public FusionRequestContext()
    {
        _features = new PooledFeatureCollection(this);
    }

    public override ISchemaDefinition Schema => _schema;

    public override ulong ExecutorVersion => _executorVersion;

    public override IOperationRequest Request => _request;

    public override int RequestIndex => _requestIndex;

    public override IServiceProvider RequestServices { get; set; } = default!;

    public override OperationDocumentInfo OperationDocumentInfo => throw new NotImplementedException();

    public override IFeatureCollection Features => throw new NotImplementedException();

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

public sealed class FusionRequestExecutor : IRequestExecutor
{
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly ObjectPool<FusionRequestContext> _contextPool;


    public FusionRequestExecutor(
        ISchemaDefinition schema,
        IServiceProvider applicationServices,
        RequestDelegate requestDelegate,
        ObjectPool<FusionRequestContext> contextPool,
        ulong version)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(applicationServices);
        ArgumentNullException.ThrowIfNull(requestDelegate);
        ArgumentNullException.ThrowIfNull(contextPool);

        _applicationServices = applicationServices;
        _requestDelegate = requestDelegate;
        _contextPool = contextPool;
        Schema = schema;
        Version = version;
    }

    public ulong Version { get; }

    public ISchemaDefinition Schema { get; }

    public Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteInternalAsync(request, null, cancellationToken);
    }

    private async Task<IExecutionResult> ExecuteInternalAsync(
        IOperationRequest request,
        int? requestIndex,
        CancellationToken cancellationToken)
    {
        IServiceScope? scope = null;
        var requestServices = request.Services;

        if (requestServices is null)
        {
            scope = request.Features.TryGet(out IServiceScopeFactory? serviceScopeFactory)
                ? serviceScopeFactory.CreateScope()
                : _applicationServices.CreateScope();
            requestServices = scope.ServiceProvider;
        }

        var context = _contextPool.Get();

        try
        {
            context.Initialize(
                Schema,
                Version,
                request,
                requestIndex ?? -1,
                requestServices,
                cancellationToken);

            await _requestDelegate(context).ConfigureAwait(false);

            if (context.Result is null)
            {
                throw new InvalidOperationException();
            }

            if (scope is null)
            {
                var localContext = context;

                if (context.Result.IsStreamResult())
                {
                    context.Result.RegisterForCleanup(() => _contextPool.Return(localContext));
                    context = null;
                }

                return localContext.Result;
            }

            if (context.Result.IsStreamResult())
            {
                var localContext = context;
                context.Result.RegisterForCleanup(scope);
                context.Result.RegisterForCleanup(() => _contextPool.Return(localContext));
                scope = null;
                context = null;
                return localContext.Result;
            }

            return context.Result;

        }
        finally
        {
            if (context is not null)
            {
                _contextPool.Return(context);
            }

            if (scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                scope?.Dispose();
            }
        }
    }

    public Task<IResponseStream> ExecuteBatchAsync(OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal sealed class RequestContextPooledObjectPolicy : PooledObjectPolicy<FusionRequestContext>
{
    public override FusionRequestContext Create()
        => new();

    public override bool Return(FusionRequestContext obj)
    {
        obj.Reset();
        return true;
    }
}
