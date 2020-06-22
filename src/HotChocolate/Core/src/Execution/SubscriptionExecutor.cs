using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.Utilities.ResolverExecutionHelper;

namespace HotChocolate.Execution
{
    internal sealed class SubscriptionExecutor
    {
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private readonly QueryExecutor _queryExecutor;
        private readonly IDiagnosticEvents _diagnosicEvents;

        public async Task<IExecutionResult> ExecuteAsync(
            IRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            if (requestContext is null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            if (requestContext.Operation is null || requestContext.Variables is null)
            {
                // TODO : throw helper
                throw new GraphQLException("InvalidContext");
            }

            IPreparedSelectionList rootSelections = requestContext.Operation.GetRootSelections();

            if (rootSelections.Count != 1)
            {
                // TODO : throw helper
                throw new GraphQLException();
            }

            if (rootSelections[0].Field.SubscribeResolver is null)
            {
                // TODO : throw helper
                throw new GraphQLException("Subscribe resolver missing!");
            }

            var subscription = new Subscription(
                _operationContextPool,
                _queryExecutor,
                requestContext,
                requestContext.Operation.RootType,
                rootSelections,
                _diagnosicEvents);

            try
            {
                // TODO : discuss with rafi
                IAsyncEnumerable<object> sourceStream =
                    await subscription.SubscribeAsync().ConfigureAwait(false);

                IAsyncEnumerable<IQueryResult> resultStream =
                    subscription.ExecuteAsync(sourceStream);

                return new SubscriptionResult(resultStream, null);
            }
            catch (Exception ex)
            {
                // todo : create error result
                throw;
            }
        }

        private sealed class Subscription
        {
            private readonly ObjectPool<OperationContext> _operationContextPool;
            private readonly QueryExecutor _queryExecutor;
            private readonly IDiagnosticEvents _diagnosicEvents;
            private readonly IRequestContext _requestContext;
            private readonly ObjectType _subscriptionType;
            private readonly IPreparedSelectionList _rootSelections;
            private object? _cachedRootValue = null;

            public Subscription(
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

            public async ValueTask<IAsyncEnumerable<object>> SubscribeAsync()
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

                    return await rootSelection.Field.SubscribeResolver!.Invoke(middlewareContext)
                        .ConfigureAwait(false);
                }
                finally
                {
                    operationContext.Result.DropResult();
                    _operationContextPool.Return(operationContext);
                }
            }

            public async IAsyncEnumerable<IQueryResult> ExecuteAsync(
                IAsyncEnumerable<object> sourceStream)
            {
                // TODO : we need a way to abort this enumeration
                IAsyncEnumerator<object> enumerator =
                    sourceStream.GetAsyncEnumerator(CancellationToken.None);

                try
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return await OnEvent(enumerator.Current).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
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
        }
    }
}
