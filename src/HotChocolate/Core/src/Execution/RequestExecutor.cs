using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class RequestExecutor : IRequestExecutor
{
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly ObjectPool<DefaultRequestContext> _contextPool;
    private readonly DefaultRequestContextAccessor _contextAccessor;
    private readonly IRequestContextEnricher[] _enricher;

    public RequestExecutor(
        Schema schema,
        IServiceProvider applicationServices,
        IServiceProvider executorServices,
        RequestDelegate requestDelegate,
        ObjectPool<DefaultRequestContext> contextPool,
        DefaultRequestContextAccessor contextAccessor,
        ulong version)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(applicationServices);
        ArgumentNullException.ThrowIfNull(executorServices);
        ArgumentNullException.ThrowIfNull(requestDelegate);
        ArgumentNullException.ThrowIfNull(contextPool);
        ArgumentNullException.ThrowIfNull(contextAccessor);

        Schema = schema;
        _applicationServices = applicationServices;
        Services = executorServices;
        _requestDelegate = requestDelegate;
        _contextPool = contextPool;
        _contextAccessor = contextAccessor;
        Version = version;

        var list = new List<IRequestContextEnricher>();
        CollectEnricher(applicationServices, list);
        CollectEnricher(executorServices, list);
        _enricher = list.ToArray();
        return;

        static void CollectEnricher(IServiceProvider services, List<IRequestContextEnricher> list)
        {
            var enricher = services.GetService<IEnumerable<IRequestContextEnricher>>();

            if (enricher is not null)
            {
                list.AddRange(enricher);
            }
        }
    }

    public Schema Schema { get; }

    public IServiceProvider Services { get; }

    public ulong Version { get; }

    public Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteAsync(request, true, null, cancellationToken);
    }

    private async Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        bool scopeDataLoader,
        int? requestIndex,
        CancellationToken cancellationToken)
    {
        IServiceScope? scope = null;

        if (request.Services is null)
        {
            if (request.ContextData?.TryGetValue(nameof(IServiceScopeFactory), out var value) ?? false)
            {
                scope = ((IServiceScopeFactory)value!).CreateScope();
            }
            else
            {
                scope = _applicationServices.CreateScope();
            }
        }

        var services = scope is null
            ? request.Services!
            : scope.ServiceProvider;

        if (scopeDataLoader)
        {
            // we ensure that at the beginning of each execution there is a fresh batching scope.
            services.InitializeDataLoaderScope();
        }

        var context = _contextPool.Get();

        try
        {
            context.Initialize(request, services);
            context.RequestAborted = cancellationToken;
            context.RequestIndex = requestIndex;
            EnrichContext(context);

            _contextAccessor.RequestContext = context;

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

            if(scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                scope?.Dispose();
            }
        }
    }

    public Task<IResponseStream> ExecuteBatchAsync(
        OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestBatch);

        return Task.FromResult<IResponseStream>(
            new ResponseStream(
                () => CreateResponseStream(requestBatch, cancellationToken),
                ExecutionResultKind.BatchResult));
    }

    private async IAsyncEnumerable<IOperationResult> CreateResponseStream(
        OperationRequestBatch requestBatch,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        IServiceScope? scope = null;

        if (requestBatch.Services is null)
        {
            if (requestBatch.ContextData?.TryGetValue(nameof(IServiceScopeFactory), out var value) ?? false)
            {
                scope = ((IServiceScopeFactory)value!).CreateScope();
            }
            else
            {
                scope = _applicationServices.CreateScope();
            }
        }

        var services = scope is null
            ? requestBatch.Services!
            : scope.ServiceProvider;

        // we ensure that at the start of each execution there is a fresh batching scope.
        services.InitializeDataLoaderScope();

        try
        {
            await foreach (var result in ExecuteBatchStream(requestBatch, services, ct).ConfigureAwait(false))
            {
                yield return result;
            }
        }
        finally
        {
            if(scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                scope?.Dispose();
            }
        }
    }

    private async IAsyncEnumerable<IOperationResult> ExecuteBatchStream(
        OperationRequestBatch requestBatch,
        IServiceProvider services,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var requests = requestBatch.Requests;
        var requestCount = requests.Count;
        var tasks = new List<Task>(requestCount);

        var completed = new ConcurrentStack<IOperationResult>();

        for (var i = 0; i < requestCount; i++)
        {
            tasks.Add(ExecuteBatchItemAsync(WithServices(requests[i], services), i, completed, ct));
        }

        var buffer = new IOperationResult[8];

        do
        {
            var resultCount = completed.TryPopRange(buffer);

            for (var i = 0; i < resultCount; i++)
            {
                yield return buffer[i];
            }

            if (completed.IsEmpty && tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);

                // we await to throw if it's not successful.
                if (task.Status is not TaskStatus.RanToCompletion)
                {
                    await task;
                }

                tasks.Remove(task);
            }
        }
        while (tasks.Count > 0 || !completed.IsEmpty);
    }

    private static IOperationRequest WithServices(IOperationRequest request, IServiceProvider services)
    {
        switch (request)
        {
            case OperationRequest operationRequest:
                return operationRequest.WithServices(services);

            case VariableBatchRequest variableBatchRequest:
                return variableBatchRequest.WithServices(services);

            default:
                throw new InvalidOperationException("Unexpected request type.");
        }
    }

    private async Task ExecuteBatchItemAsync(
        IOperationRequest request,
        int requestIndex,
        ConcurrentStack<IOperationResult> completed,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(request, false, requestIndex, cancellationToken).ConfigureAwait(false);
        await UnwrapBatchItemResultAsync(result, completed, cancellationToken);
    }

    private static async Task UnwrapBatchItemResultAsync(
        IExecutionResult result,
        ConcurrentStack<IOperationResult> completed,
        CancellationToken cancellationToken)
    {
        switch (result)
        {
            case OperationResult singleResult:
                completed.Push(singleResult);
                break;

            case IResponseStream stream:
            {
                await foreach (var item in stream.ReadResultsAsync().WithCancellation(cancellationToken))
                {
                    completed.Push(item);
                }

                break;
            }

            case OperationResultBatch resultBatch:
            {
                List<Task>? tasks = null;
                foreach (var item in resultBatch.Results)
                {
                    if (item is OperationResult singleItem)
                    {
                        completed.Push(singleItem);
                    }
                    else
                    {
                        (tasks ??= []).Add(UnwrapBatchItemResultAsync(item, completed, cancellationToken));
                    }
                }

                if (tasks is not null)
                {
                    await Task.WhenAll(tasks);
                }

                break;
            }

            default:
                throw new InvalidOperationException();
        }
    }

    private void EnrichContext(IRequestContext context)
    {
        if (_enricher.Length == 0)
        {
            return;
        }

        ref var start = ref MemoryMarshal.GetArrayDataReference(_enricher);
        ref var end = ref Unsafe.Add(ref start, _enricher.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            start.Enrich(context);

#pragma warning disable CS8619
            start = ref Unsafe.Add(ref start, 1);
#pragma warning restore CS8619
        }
    }
}
