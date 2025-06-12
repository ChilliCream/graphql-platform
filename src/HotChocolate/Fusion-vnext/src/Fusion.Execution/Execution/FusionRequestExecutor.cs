using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestExecutor : IRequestExecutor
{
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly ObjectPool<PooledRequestContext> _contextPool;

    public FusionRequestExecutor(
        ISchemaDefinition schema,
        IServiceProvider applicationServices,
        RequestDelegate requestDelegate,
        ObjectPool<PooledRequestContext> contextPool,
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
