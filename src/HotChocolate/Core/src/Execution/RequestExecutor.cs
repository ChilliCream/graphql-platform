using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Batching;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class RequestExecutor : IRequestExecutor
{
    private readonly IServiceProvider _applicationServices;
    private readonly RequestDelegate _requestDelegate;
    private readonly BatchExecutor _batchExecutor;
    private readonly ObjectPool<RequestContext> _contextPool;
    private readonly DefaultRequestContextAccessor _contextAccessor;
    private readonly IRequestContextEnricher[] _enricher;

    public RequestExecutor(
        ISchema schema,
        IServiceProvider applicationServices,
        IServiceProvider executorServices,
        RequestDelegate requestDelegate,
        BatchExecutor batchExecutor,
        ObjectPool<RequestContext> contextPool,
        DefaultRequestContextAccessor contextAccessor,
        ulong version)
    {
        Schema = schema ??
            throw new ArgumentNullException(nameof(schema));
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
        _contextAccessor = contextAccessor ?? 
            throw new ArgumentNullException(nameof(contextAccessor));
        Version = version;

        var list = new List<IRequestContextEnricher>();
        CollectEnricher(applicationServices, list);
        CollectEnricher(executorServices, list);
        _enricher = list.ToArray();

        static void CollectEnricher(IServiceProvider services, List<IRequestContextEnricher> list)
        {
            var enricher = services.GetService<IEnumerable<IRequestContextEnricher>>();

            if (enricher is not null)
            {
                list.AddRange(enricher);
            }
        }
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

        var scope = request.Services is null
            ? _applicationServices.CreateScope()
            : null;

        var services = scope is null
            ? request.Services!
            : scope.ServiceProvider;

        var context = _contextPool.Get();

        try
        {
            context.RequestAborted = cancellationToken;
            context.Initialize(request, services);
            EnrichContext(context);
            
            _contextAccessor.RequestContext = context;

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

    private void EnrichContext(IRequestContext context)
    {
        if (_enricher.Length > 0)
        {
#if NET6_0_OR_GREATER
            ref var start = ref MemoryMarshal.GetArrayDataReference(_enricher);
            ref var end = ref Unsafe.Add(ref start, _enricher.Length);
#else
            ref var start = ref MemoryMarshal.GetReference(_enricher.AsSpan());
            ref var end = ref Unsafe.Add(ref start, _enricher.Length);
#endif

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                start.Enrich(context);

#pragma warning disable CS8619
                start = ref Unsafe.Add(ref start, 1);
#pragma warning restore CS8619
            }
        }
    }
}
