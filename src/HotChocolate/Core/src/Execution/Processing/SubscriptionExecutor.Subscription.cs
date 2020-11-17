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

            // subscribe will use the subscribe resolver to create a source stream that yields
            // the event messages from the underlying pub/sub-system.
            private async ValueTask<ISourceStream> SubscribeAsync()
            {
                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    // first we will create the root value which essentially
                    // is the subscription object. In some cases this object is null.
                    object? rootValue = RootValueResolver.TryResolve(
                        _requestContext,
                        _requestContext.Services,
                        _subscriptionType,
                        ref _cachedRootValue);

                    // next we need to initialize our operation context so that we have access to
                    // variables services and other things.
                    // The subscribe resolver will use a noop dispatcher and all DataLoader are 
                    // dispatched immediately.
                    operationContext.Initialize(
                        _requestContext,
                        _requestContext.Services,
                        NoopBatchDispatcher.Default,
                        _requestContext.Operation!,
                        rootValue,
                        _requestContext.Variables!);

                    // next we need a result map so that we can store the subscribe temporarily
                    // while executing the subscribe pipeline.
                    ResultMap resultMap = operationContext.Result.RentResultMap(1);
                    ISelection rootSelection = _rootSelections.Selections[0];

                    // we create a temporary middleware context so that we can use the standard
                    // resolver pipeline.
                    var middlewareContext = new MiddlewareContext();
                    middlewareContext.Initialize(
                        operationContext,
                        rootSelection,
                        resultMap,
                        1,
                        rootValue,
                        Path.New(rootSelection.ResponseName),
                        ImmutableDictionary<string, object?>.Empty);

                    // it is important that we correctly coerce the arguments before
                    // invoking subscribe.
                    if (!rootSelection.Arguments.TryCoerceArguments(
                        middlewareContext.Variables,
                        middlewareContext.ReportError,
                        out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
                    {
                        // the middleware context reports errors to the operation context,
                        // this means if we failed, we need to grab the coercion errors from there
                        // and just throw a GraphQLException.
                        throw new GraphQLException(operationContext.Result.Errors);
                    }

                    // if everything is fine with the arguments we still need to assign them.
                    middlewareContext.Arguments = coercedArgs;

                    // last but not least we can invoke the subscribe resolver which will subscribe
                    // to the underlying pub/sub-system yielding the source stream.
                    ISourceStream sourceStream =
                        await rootSelection.Field.SubscribeResolver!.Invoke(middlewareContext)
                            .ConfigureAwait(false);

                    if (operationContext.Result.Errors.Count > 0)
                    {
                        // again if we have any errors we will just throw them and not opening
                        // any subscription context.
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
