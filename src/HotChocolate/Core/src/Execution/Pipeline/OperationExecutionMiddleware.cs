using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationExecutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private readonly QueryExecutor _queryExecutor;
        private readonly MutationExecutor _mutationExecutor;
        private readonly SubscriptionExecutor _subscriptionExecutor;
        private object? _cachedQueryValue;
        private object? _cachedMutation;

        public OperationExecutionMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            ObjectPool<OperationContext> operationContextPool,
            QueryExecutor queryExecutor,
            MutationExecutor mutationExecutor,
            SubscriptionExecutor subscriptionExecutor)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _operationContextPool = operationContextPool ??
                throw new ArgumentNullException(nameof(operationContextPool));
            _queryExecutor = queryExecutor ??
                throw new ArgumentNullException(nameof(queryExecutor));
            _mutationExecutor = mutationExecutor ??
                throw new ArgumentNullException(nameof(mutationExecutor));
            _subscriptionExecutor = subscriptionExecutor ??
                throw new ArgumentNullException(nameof(subscriptionExecutor));
        }

        public async ValueTask InvokeAsync(
            IRequestContext context,
            IBatchDispatcher? batchDispatcher)
        {
            if (batchDispatcher is null)
            {
                throw OperationExecutionMiddleware_NoBatchDispatcher();
            }

            if (context.Operation is { } && context.Variables is { })
            {
                if (context.Operation.Definition.Operation == OperationType.Subscription)
                {
                    context.Result = await _subscriptionExecutor
                        .ExecuteAsync(context)
                        .ConfigureAwait(false);

                    await _next(context).ConfigureAwait(false);
                }
                else
                {
                    OperationContext? operationContext = _operationContextPool.Get();

                    try
                    {
                        IQueryResult? result = null;

                        if (context.Operation.Definition.Operation == OperationType.Query)
                        {
                            object? query = RootValueResolver.TryResolve(
                                context,
                                context.Services,
                                context.Operation.RootType,
                                ref _cachedQueryValue);

                            operationContext.Initialize(
                                context,
                                context.Services,
                                batchDispatcher,
                                context.Operation,
                                query,
                                context.Variables);

                            result = await _queryExecutor
                                .ExecuteAsync(operationContext)
                                .ConfigureAwait(false);
                        }
                        else if (context.Operation.Definition.Operation == OperationType.Mutation)
                        {
                            object? mutation = RootValueResolver.TryResolve(
                                context,
                                context.Services,
                                context.Operation.RootType,
                                ref _cachedMutation);

                            operationContext.Initialize(
                                context,
                                context.Services,
                                batchDispatcher,
                                context.Operation,
                                mutation,
                                context.Variables);

                            result = await _mutationExecutor
                                .ExecuteAsync(operationContext)
                                .ConfigureAwait(false);
                        }

                        if (operationContext.Execution.DeferredTaskBacklog.IsEmpty || 
                            result is null)
                        {
                            context.Result = result;
                        }
                        else
                        {
                            // if we have deferred query task we will take ownership
                            // of the life time handling and return the operation context
                            // once we handled all deferred tasks.
                            var operationContextOwner = new OperationContextOwner(
                                operationContext, _operationContextPool);
                            operationContext = null;

                            context.Result = new DeferredResult
                            (
                                result,
                                new DeferredTaskExecutor(operationContextOwner),
                                session: operationContextOwner
                            );
                        }

                        await _next(context).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (operationContext is not null)
                        {
                            _operationContextPool.Return(operationContext);
                        }
                    }
                }
            }
            else
            {
                context.Result = ErrorHelper.StateInvalidForOperationExecution();
            }
        }
    }
}
