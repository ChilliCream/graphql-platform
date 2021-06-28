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
        private readonly ITransactionScopeHandler _transactionScopeHandler;
        private object? _cachedQuery;
        private object? _cachedMutation;

        public OperationExecutionMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            ObjectPool<OperationContext> operationContextPool,
            QueryExecutor queryExecutor,
            MutationExecutor mutationExecutor,
            SubscriptionExecutor subscriptionExecutor,
            [SchemaService] ITransactionScopeHandler transactionScopeHandler)
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
            _transactionScopeHandler = transactionScopeHandler ??
                throw new ArgumentNullException(nameof(transactionScopeHandler));
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
                    await ExecuteQueryOrMutationAsync(
                        context, batchDispatcher, operation, operationContext)
                        .ConfigureAwait(false);

                    if(!operationContext.Execution.DeferredTaskBacklog.IsEmpty &&
                       context.Result is IQueryResult result)
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

        private async Task ExecuteQueryOrMutationAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher,
            IPreparedOperation operation,
            OperationContext operationContext)
        {
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

                context.Result = await _queryExecutor
                    .ExecuteAsync(operationContext)
                    .ConfigureAwait(false);
            }
            else if (operation.Definition.Operation == OperationType.Mutation)
            {
                using ITransactionScope transactionScope =
                    _transactionScopeHandler.Create(context);

                object? mutation = GetMutationRootValue(context);

                operationContext.Initialize(
                    context,
                    context.Services,
                    batchDispatcher,
                    operation,
                    context.Variables!,
                    mutation,
                    () => GetQueryRootValue(context));

                context.Result = await _mutationExecutor
                    .ExecuteAsync(operationContext)
                    .ConfigureAwait(false);

                transactionScope.Complete();
            }
        }

        private object? GetQueryRootValue(IRequestContext context) =>
            RootValueResolver.Resolve(
                context,
                context.Services,
                context.Schema.QueryType,
                ref _cachedQuery);

        private object? GetMutationRootValue(IRequestContext context) =>
            RootValueResolver.Resolve(
                context,
                context.Services,
                context.Schema.MutationType!,
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
