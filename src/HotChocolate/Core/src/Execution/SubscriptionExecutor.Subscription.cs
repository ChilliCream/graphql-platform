using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed partial class SubscriptionExecutor
    {
        private sealed class Subscription
            : IAsyncDisposable
        {
            private readonly ObjectPool<OperationContext> _operationContextPool;
            private readonly QueryExecutor _queryExecutor;
            private readonly IDiagnosticEvents _diagnosicEvents;
            private readonly IRequestContext _requestContext;
            private readonly ObjectType _subscriptionType;
            private readonly IPreparedSelectionList _rootSelections;
            private ISourceStream _sourceStream = default!;
            private object? _cachedRootValue = null;
            private bool _disposed;

            private Subscription(
                ObjectPool<OperationContext> operationContextPool,
                QueryExecutor queryExecutor,
                IRequestContext requestContext,
                ObjectType subscriptionType,
                IPreparedSelectionList rootSelections,
                IDiagnosticEvents diagnosicEvents)
            {
                _operationContextPool = operationContextPool;
                _queryExecutor = queryExecutor;
                _requestContext = requestContext;
                _subscriptionType = subscriptionType;
                _rootSelections = rootSelections;
                _diagnosicEvents = diagnosicEvents;
            }

            public static async ValueTask<Subscription> SubscribeAsync(
                ObjectPool<OperationContext> operationContextPool,
                QueryExecutor queryExecutor,
                IRequestContext requestContext,
                ObjectType subscriptionType,
                IPreparedSelectionList rootSelections,
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

            private async Task<IReadOnlyQueryResult> OnEvent(object payload)
            {
                using IServiceScope serviceScope = _requestContext.Services.CreateScope();

                IServiceProvider eventServices = serviceScope.ServiceProvider;
                IBatchDispatcher dispatcher = eventServices.GetRequiredService<IBatchDispatcher>();

                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    var scopedContext = ImmutableDictionary<string, object?>.Empty
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

                    return await _queryExecutor.ExecuteAsync(operationContext, scopedContext)
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
                    IPreparedSelection rootSelection = _rootSelections[0];

                    var middlewareContext = new MiddlewareContext();
                    middlewareContext.Initialize(
                        operationContext,
                        _rootSelections[0],
                        resultMap,
                        1,
                        rootValue,
                        Path.New(_rootSelections[0].ResponseName),
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
