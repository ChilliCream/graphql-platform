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

    private int _activeRequests;

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

    public int ActiveRequests => _activeRequests;

    public async Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        Interlocked.Increment(ref _activeRequests);

        IExecutionResult? result = default;
        try
        {
            result = await ExecuteSingleAsync(request, cancellationToken);
            return result;
        }
        finally
        {
            DecrementActiveRequests(result);
        }
    }

    public Task<IResponseStream> ExecuteBatchAsync(
        IEnumerable<IQueryRequest> requestBatch,
        bool allowParallelExecution = false,
        CancellationToken cancellationToken = default)
    {
        if (requestBatch is null)
        {
            throw new ArgumentNullException(nameof(requestBatch));
        }

        Interlocked.Increment(ref _activeRequests);
        ResponseStream? responseStream = default;
        try
        {
            responseStream = new ResponseStream(() => _batchExecutor.ExecuteAsync(this, requestBatch),
                ExecutionResultKind.BatchResult);

            return Task.FromResult<IResponseStream>(responseStream);
        }
        finally
        {
            DecrementActiveRequests(responseStream);
        }
    }

    private async Task<IExecutionResult> ExecuteSingleAsync(IQueryRequest request, CancellationToken cancellationToken)
    {
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

    private void DecrementActiveRequests(IExecutionResult? result)
    {
        if (result == null || !result.IsStreamResult())
        {
            Interlocked.Decrement(ref _activeRequests);
            return;
        }

        result.RegisterForCleanup(new DisposableAction(this));
    }

    private sealed class DisposableAction : IDisposable
    {
        private bool _disposed;
        private RequestExecutor? _executor;

        public DisposableAction(RequestExecutor executor)
        {
            _executor = executor;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DisposableAction()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (_executor is not null)
            {
                Interlocked.Decrement(ref _executor._activeRequests);
                _executor = default;
            }

            _disposed = true;
        }
    }
}
