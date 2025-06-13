using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class DefaultRequestExecutor : IRequestExecutor
{
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly ObjectPool<PooledRequestContext> _contextPool;
    private readonly DefaultRequestContextAccessor _contextAccessor;
    private readonly IRequestContextEnricher[] _enricher;
    private List<Task>? _taskList;

    public DefaultRequestExecutor(
        ISchemaDefinition schema,
        IServiceProvider applicationServices,
        RequestDelegate requestDelegate,
        ObjectPool<PooledRequestContext> requestContextPool,
        DefaultRequestContextAccessor contextAccessor,
        ulong version)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(applicationServices);
        ArgumentNullException.ThrowIfNull(requestContextPool);
        ArgumentNullException.ThrowIfNull(contextAccessor);

        Schema = schema;
        Version = version;

        _requestDelegate = requestDelegate;
        _applicationServices = applicationServices;
        _contextPool = requestContextPool;
        _contextAccessor = contextAccessor;

        var list = new List<IRequestContextEnricher>();
        CollectEnricher(applicationServices, list);
        CollectEnricher(schema.Services, list);
        _enricher = [.. list];

        static void CollectEnricher(IServiceProvider services, List<IRequestContextEnricher> list)
        {
            var enricher = services.GetService<IEnumerable<IRequestContextEnricher>>();

            if (enricher is not null)
            {
                list.AddRange(enricher);
            }
        }
    }

    /// <summary>
    /// Gets the schema definition that this request executor is configured for.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the version of the request executor.
    /// </summary>
    public ulong Version { get; }

    /// <summary>
    /// Executes a single GraphQL <see cref="IOperationRequest"/> and returns the
    /// <see cref="IExecutionResult"/> produced by the configured request pipeline.
    /// </summary>
    /// <param name="request">
    /// The operation request to execute.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the execution.
    /// </param>
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
        var requestServices = request.Services;

        if (requestServices is null)
        {
            scope = request.Features.TryGet(out IServiceScopeFactory? serviceScopeFactory)
                ? serviceScopeFactory.CreateScope()
                : _applicationServices.CreateScope();
            requestServices = scope.ServiceProvider;
        }

        if (scopeDataLoader)
        {
            // we ensure that at the beginning of each execution there is a fresh batching scope.
            requestServices.InitializeDataLoaderScope();
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

            EnrichContext(context);

            _contextAccessor.RequestContext = context;

            await _requestDelegate(context).ConfigureAwait(false);

            if (context.Result is null)
            {
                throw new InvalidOperationException(
                    "The request pipeline is expected to produce an execution result.");
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
            // prevent stale request context from leaking after the pooled instance is returned.
            _contextAccessor.RequestContext = null!;

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

    /// <summary>
    /// Executes a batch of GraphQL operation requests and returns an
    /// <see cref="IResponseStream"/> that yields each individual
    /// <see cref="IOperationResult"/> as it becomes available.
    /// </summary>
    /// <param name="requestBatch">
    /// The batch of operation requests.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the execution.
    /// </param>
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
        var requestServices = requestBatch.Services;

        if (requestServices is null)
        {
            scope = requestBatch.Features.TryGet(out IServiceScopeFactory? serviceScopeFactory)
                ? serviceScopeFactory.CreateScope()
                : _applicationServices.CreateScope();
            requestServices = scope.ServiceProvider;
        }

        // we ensure that at the start of each execution, there is a fresh batching scope.
        requestServices.InitializeDataLoaderScope();

        try
        {
            await foreach (var result in ExecuteBatchStream(requestBatch, requestServices, ct).ConfigureAwait(false))
            {
                yield return result;
            }
        }
        finally
        {
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

    private async IAsyncEnumerable<IOperationResult> ExecuteBatchStream(
        OperationRequestBatch requestBatch,
        IServiceProvider services,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var requests = requestBatch.Requests;
        var requestCount = requests.Count;
        var tasks = Interlocked.Exchange(ref _taskList, null) ?? new List<Task>(requestCount);

        var completed = new List<IOperationResult>();

        for (var i = 0; i < requestCount; i++)
        {
            tasks.Add(ExecuteBatchItemAsync(WithServices(requests[i], services), i, completed, ct));
        }

        var buffer = new IOperationResult[Math.Min(16, requestCount)];

        while (tasks.Count > 0 || completed.Count > 0)
        {
            var count = completed.TryDequeueRange(buffer);

            for (var i = 0; i < count; i++)
            {
                yield return buffer[i];
            }

            if (completed.Count == 0 && tasks.Count > 0)
            {
                await Task.WhenAny(tasks).ConfigureAwait(false);

                for (var i = tasks.Count - 1; i >= 0; i--)
                {
                    var promise = tasks[i];
                    if (!promise.IsCompleted)
                    {
                        continue;
                    }

                    if (!promise.IsCompletedSuccessfully)
                    {
                        await promise.ConfigureAwait(false);
                    }

                    tasks.RemoveAt(i);
                }
            }
        }

        // if our set is not overly large, we try to reuse it
        // for the next batch execution.
        if (requestCount <= 1024)
        {
            // The tasks HashSet is assumed be empty,
            // otherwise we would not have exited the loop.
            // So it's safe to reuse it without clearing it.
            Interlocked.CompareExchange(ref _taskList, tasks, null);
        }
    }

    private static IOperationRequest WithServices(
        IOperationRequest request,
        IServiceProvider services) =>
        request switch
        {
            OperationRequest op => op.WithServices(services),
            VariableBatchRequest vb => vb.WithServices(services),
            _ => throw new InvalidOperationException("Unexpected request type.")
        };

    private async Task ExecuteBatchItemAsync(
        IOperationRequest request,
        int requestIndex,
        List<IOperationResult> completed,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(request, false, requestIndex, cancellationToken).ConfigureAwait(false);
        await UnwrapBatchItemResultAsync(result, completed, cancellationToken);
    }

    private static async Task UnwrapBatchItemResultAsync(
        IExecutionResult result,
        List<IOperationResult> completed,
        CancellationToken cancellationToken)
    {
        switch (result)
        {
            case OperationResult singleResult:
                completed.Enqueue(singleResult);
                break;

            case IResponseStream stream:
            {
                await foreach (var item in stream.ReadResultsAsync().WithCancellation(cancellationToken))
                {
                    completed.Enqueue(item);
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
                        completed.Enqueue(singleItem);
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
                throw new InvalidOperationException(
                    "The request pipeline is expected to produce an execution result.");
        }
    }

    private void EnrichContext(RequestContext context)
    {
        if (_enricher.Length == 0)
        {
            return;
        }

        foreach (var enricher in _enricher)
        {
            enricher.Enrich(context);
        }
    }
}

file static class ListExtensions
{
    public static void Enqueue<T>(this List<T> queue, T item)
    {
        lock (queue)
        {
            queue.Insert(0, item);
        }
    }

    public static int TryDequeueRange<T>(this List<T> queue, T[] buffer)
    {
        lock (queue)
        {
            var count = Math.Min(queue.Count, buffer.Length);
            var j = 0;

            for (var i = count - 1; i >= 0; i--)
            {
                buffer[j++] = queue[i];
                queue.RemoveAt(i);
            }

            return count;
        }
    }
}
