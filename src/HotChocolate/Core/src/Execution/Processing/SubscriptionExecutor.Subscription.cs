using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class SubscriptionExecutor
    {
        private sealed class Subscription
            : IAsyncDisposable
        {
            private readonly ObjectPool<OperationContext> _operationContextPool;
            private readonly QueryExecutor _queryExecutor;
            private readonly IDiagnosticEvents _diagnosticEvents;
            private readonly IRequestContext _requestContext;
            private readonly ObjectType _subscriptionType;
            private readonly ISelectionSet _rootSelections;
            private ISourceStream _sourceStream = default!;
            private object? _cachedRootValue = null;
            private bool _disposed;

            private Subscription(
                ObjectPool<OperationContext> operationContextPool,
                QueryExecutor queryExecutor,
                IRequestContext requestContext,
                ObjectType subscriptionType,
                ISelectionSet rootSelections,
                IDiagnosticEvents diagnosticEvents)
            {
                _operationContextPool = operationContextPool;
                _queryExecutor = queryExecutor;
                _requestContext = requestContext;
                _subscriptionType = subscriptionType;
                _rootSelections = rootSelections;
                _diagnosticEvents = diagnosticEvents;
            }

            public static async ValueTask<Subscription> SubscribeAsync(
                ObjectPool<OperationContext> operationContextPool,
                QueryExecutor queryExecutor,
                IRequestContext requestContext,
                ObjectType subscriptionType,
                ISelectionSet rootSelections,
                IDiagnosticEvents diagnosicEvents)
            {
                var subscription = new Subscription(
                    operationContextPool,
                    queryExecutor,
                    requestContext,
                    subscriptionType,
                    rootSelections,
                    diagnosicEvents);

                subscription._sourceStream = await subscription.SubscribeAsync();

                return subscription;
            }

            public async IAsyncEnumerable<IQueryResult> ExecuteAsync()
            {
                await using IAsyncEnumerator<object> enumerator =
                    _sourceStream.ReadEventsAsync().GetAsyncEnumerator(
                        _requestContext.RequestAborted);

                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (_requestContext.RequestAborted.IsCancellationRequested)
                    {
                        break;
                    }

                    yield return await OnEvent(enumerator.Current).ConfigureAwait(false);
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    await _sourceStream.DisposeAsync().ConfigureAwait(false);
                    _disposed = true;
                }
            }

            private async Task<IQueryResult> OnEvent(object payload)
            {
                using IServiceScope serviceScope = _requestContext.Services.CreateScope();

                IServiceProvider eventServices = serviceScope.ServiceProvider;
                IBatchDispatcher dispatcher = eventServices.GetRequiredService<IBatchDispatcher>();

                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    ImmutableDictionary<string, object?> scopedContext =
                        ImmutableDictionary<string, object?>.Empty
                            .SetItem(WellKnownContextData.EventMessage, payload);

                    object? rootValue = RootValueResolver.TryResolve(
                        _requestContext,
                        eventServices,
                        _subscriptionType,
                        ref _cachedRootValue);

                    operationContext.Initialize(
                        _requestContext,
                        eventServices,
                        dispatcher,
                        _requestContext.Operation!,
                        rootValue,
                        _requestContext.Variables!);

                    return await _queryExecutor
                        .ExecuteAsync(operationContext, scopedContext)
                        .ConfigureAwait(false);
                }
                finally
                {
                    _operationContextPool.Return(operationContext);
                }
            }

            private async ValueTask<ISourceStream> SubscribeAsync()
            {
                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    object? rootValue = RootValueResolver.TryResolve(
                        _requestContext,
                        _requestContext.Services,
                        _subscriptionType,
                        ref _cachedRootValue);

                    operationContext.Initialize(
                        _requestContext,
                        _requestContext.Services,
                        NoopBatchDispatcher.Default,
                        _requestContext.Operation!,
                        rootValue,
                        _requestContext.Variables!);

                    ResultMap resultMap = operationContext.Result.RentResultMap(1);
                    ISelection rootSelection = _rootSelections.Selections[0];

                    var middlewareContext = new MiddlewareContext();
                    middlewareContext.Initialize(
                        operationContext,
                        _rootSelections.Selections[0],
                        resultMap,
                        1,
                        rootValue,
                        Path.New(_rootSelections.Selections[0].ResponseName),
                        ImmutableDictionary<string, object?>.Empty);

                    ISourceStream sourceStream =
                        await rootSelection.Field.SubscribeResolver!.Invoke(middlewareContext)
                            .ConfigureAwait(false);

                    if (operationContext.Result.Errors.Count > 0)
                    {
                        throw new GraphQLException(operationContext.Result.Errors);
                    }

                    return sourceStream;
                }
                finally
                {
                    operationContext.Result.DropResult();
                    _operationContextPool.Return(operationContext);
                }
            }
        }
    }
}
