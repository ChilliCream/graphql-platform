using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Batching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class RequestExecutor : IRequestExecutor
{
    private readonly DefaultRequestContextAccessor _requestContextAccessor;
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly BatchExecutor _batchExecutor;
    private readonly ObjectPool<RequestContext> _contextPool;
    private readonly bool _parallelBatching;

    public RequestExecutor(
        ISchema schema,
        DefaultRequestContextAccessor requestContextAccessor,
        IServiceProvider applicationServices,
        IServiceProvider executorServices,
        RequestDelegate requestDelegate,
        BatchExecutor batchExecutor,
        ObjectPool<RequestContext> contextPool,
        ulong version)
    {
        Schema = schema ??
            throw new ArgumentNullException(nameof(schema));
        _requestContextAccessor = requestContextAccessor ??
            throw new ArgumentNullException(nameof(requestContextAccessor));
        _applicationServices = applicationServices ??
            throw new ArgumentNullException(nameof(applicationServices));
        Services = executorServices ??
            throw new ArgumentNullException(nameof(executorServices));
        _requestDelegate = requestDelegate ??
            throw new ArgumentNullException(nameof(requestDelegate));
        _batchExecutor = batchExecutor ??
            throw new ArgumentNullException(nameof(batchExecutor));
        _contextPool = contextPool ??
            throw new ArgumentNullException(nameof(contextPool));
        Version = version;
    }

    public ISchema Schema { get; }

    public IServiceProvider Services { get; }

    public ulong Version { get; }

    public async Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        IServiceScope? scope = request.Services is null
            ? _applicationServices.CreateScope()
            : null;

        IServiceProvider services = scope is null
            ? request.Services!
            : scope.ServiceProvider;

        RequestContext context = _contextPool.Get();

        try
        {
            context.RequestAborted = cancellationToken;
            context.Initialize(request, services);

            _requestContextAccessor.RequestContext = context;

            await _requestDelegate(context).ConfigureAwait(false);

            if (context.Result is null)
            {
                throw new InvalidOperationException();
            }

            if (scope is null)
            {
                return context.Result;
            }

            if (context.Result.IsStreamResult())
            {
                context.Result.RegisterForCleanup(scope);
                scope = null;
            }

            return context.Result;
        }
        finally
        {
            _contextPool.Return(context);
            scope?.Dispose();
        }
    }

    public Task<IResponseStream> ExecuteBatchAsync(
        IReadOnlyList<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default)
    {
        if (requestBatch is null)
        {
            throw new ArgumentNullException(nameof(requestBatch));
        }

        return Task.FromResult<IResponseStream>(
            new ResponseStream(
                () => _batchExecutor.ExecuteAsync(this, requestBatch),
                ExecutionResultKind.BatchResult));
    }
}
