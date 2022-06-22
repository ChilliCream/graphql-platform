using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObjectPool<OperationContext> _operationContextPool;
    private readonly QueryExecutor _queryExecutor;
    private readonly SubscriptionExecutor _subscriptionExecutor;
    private readonly ITransactionScopeHandler _transactionScopeHandler;
    private object? _cachedQuery;
    private object? _cachedMutation;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        ObjectPool<OperationContext> operationContextPool,
        QueryExecutor queryExecutor,
        SubscriptionExecutor subscriptionExecutor,
        [SchemaService] ITransactionScopeHandler transactionScopeHandler)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _operationContextPool = operationContextPool ??
            throw new ArgumentNullException(nameof(operationContextPool));
        _queryExecutor = queryExecutor ??
            throw new ArgumentNullException(nameof(queryExecutor));
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

        if (context.Operation is not null && context.Variables is not null)
        {
            if (IsOperationAllowed(context))
            {
                using (context.DiagnosticEvents.ExecuteOperation(context))
                {
                    await ExecuteOperationAsync(context, batchDispatcher, context.Operation)
                        .ConfigureAwait(false);
                }
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
        IOperation operation)
    {
        if (operation.Definition.Operation == OperationType.Subscription)
        {
            // since the context is pooled we need to clone the context for
            // long running executions.
            var cloned = context.Clone();

            var accessor = cloned.Services.GetRequiredService<DefaultRequestContextAccessor>();
            accessor.RequestContext = cloned;

            context.Result = await _subscriptionExecutor
                .ExecuteAsync(cloned, () => GetQueryRootValue(cloned))
                .ConfigureAwait(false);

            await _next(cloned).ConfigureAwait(false);
        }
        else
        {
            var operationContext = _operationContextPool.Get();

            try
            {
                await ExecuteQueryOrMutationAsync(
                    context, batchDispatcher, operation, operationContext)
                    .ConfigureAwait(false);

                if (operationContext.Scheduler.DeferredWork.HasWork &&
                    context.Result is IQueryResult result)
                {
                    // if we have deferred query task we will take ownership
                    // of the life time handling and return the operation context
                    // once we handled all deferred tasks.
                    var operationContextOwner = new OperationContextOwner(
                        operationContext, _operationContextPool);

                    // since the context is pooled we need to clone the context for
                    // long running executions.
                    operationContext.RequestContext = context.Clone();

                    // also we set operation context to null so that it is not
                    // given back to the pool.
                    operationContext = null;

                    context.Result = new ResponseStream(
                        () => new DeferredTaskExecutor(result, operationContextOwner),
                        ExecutionResultKind.DeferredResult);
                    context.Result.RegisterForCleanup(result);
                    context.Result.RegisterForCleanup(operationContextOwner);
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
        IOperation operation,
        OperationContext operationContext)
    {
        if (operation.Definition.Operation == OperationType.Query)
        {
            var query = GetQueryRootValue(context);

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
            using var transactionScope =
                _transactionScopeHandler.Create(context);

            var mutation = GetMutationRootValue(context);

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                operation,
                context.Variables!,
                mutation,
                () => GetQueryRootValue(context));

            context.Result = await _queryExecutor
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
        var allowedOps = context.Request.AllowedOperations;

        if (allowedOps is null ||
            allowedOps.Length == 0)
        {
            return true;
        }

        if (allowedOps.Length == 1 &&
            allowedOps[0] == context.Operation?.Type)
        {
            return true;
        }

        for (var i = 0; i < allowedOps.Length; i++)
        {
            if (allowedOps[i] == context.Operation?.Type)
            {
                return true;
            }
        }

        return false;
    }
}
