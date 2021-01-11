using System;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
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

            if (context is { Operation: not null, Variables: not null })
            {
                if (IsOperationAllowed(context))
                {
                    await ExecuteOperationAsync(
                        context, batchDispatcher, context.Operation)
                        .ConfigureAwait(false);
                }
                else
                {
                    context.Result = ErrorHelper.OperationKindNotAllowed();
                }
            }
            else
            {
                context.Result = ErrorHelper.StateInvalidForOperationExecution();
            }
        }

        private async Task ExecuteOperationAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher,
            IPreparedOperation operation)
        {
            if (operation.Definition.Operation == OperationType.Subscription)
            {
                context.Result = await _subscriptionExecutor
                    .ExecuteAsync(context, () => GetQueryRootValue(context))
                    .ConfigureAwait(false);

                await _next(context).ConfigureAwait(false);
            }
            else
            {
                OperationContext? operationContext = _operationContextPool.Get();

                try
                {
                    IQueryResult? result = await ExecuteQueryOrMutationAsync(
                        context, batchDispatcher, operation, operationContext)
                        .ConfigureAwait(false);

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

                        context.Result = new DeferredQueryResult
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

        private async Task<IQueryResult?> ExecuteQueryOrMutationAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher,
            IPreparedOperation operation,
            OperationContext operationContext)
        {
            IQueryResult? result = null;

            if (operation.Definition.Operation == OperationType.Query)
            {
                object? query = GetQueryRootValue(context);

                operationContext.Initialize(
                    context,
                    context.Services,
                    batchDispatcher,
                    operation,
                    context.Variables!,
                    query,
                    () => query);

                result = await _queryExecutor
                    .ExecuteAsync(operationContext)
                    .ConfigureAwait(false);
            }
            else if (operation.Definition.Operation == OperationType.Mutation)
            {
                object? mutation = GetMutationRootValue(context);

                operationContext.Initialize(
                    context,
                    context.Services,
                    batchDispatcher,
                    operation,
                    context.Variables!,
                    mutation,
                    () => GetQueryRootValue(context));

                result = await _mutationExecutor
                    .ExecuteAsync(operationContext)
                    .ConfigureAwait(false);
            }

            return result;
        }

        private object? GetQueryRootValue(IRequestContext context) =>
            RootValueResolver.Resolve(
                context,
                context.Services,
                context.Schema.QueryType,
                ref _cachedQueryValue);

        private object? GetMutationRootValue(IRequestContext context) =>
            RootValueResolver.Resolve(
                context,
                context.Services,
                context.Schema.MutationType,
                ref _cachedMutation);

        private bool IsOperationAllowed(IRequestContext context)
        {
            if (context.Request.AllowedOperations is null or { Length: 0 })
            {
                return true;
            }

            if (context.Request.AllowedOperations is { Length: 1 } allowed &&
                allowed[0] == context.Operation?.Type)
            {
                return true;
            }

            for (var i = 0; i < context.Request.AllowedOperations.Length; i++)
            {
                if (context.Request.AllowedOperations[i] == context.Operation?.Type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
