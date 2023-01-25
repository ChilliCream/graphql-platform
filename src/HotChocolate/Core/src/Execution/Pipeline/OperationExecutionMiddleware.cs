using System;
using System.Threading.Tasks;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.GraphQLRequestFlags;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFactory<OperationContextOwner> _contextFactory;
    private readonly QueryExecutor _queryExecutor;
    private readonly SubscriptionExecutor _subscriptionExecutor;
    private readonly ITransactionScopeHandler _transactionScopeHandler;
    private object? _cachedQuery;
    private object? _cachedMutation;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        IFactory<OperationContextOwner> contextFactory,
        QueryExecutor queryExecutor,
        SubscriptionExecutor subscriptionExecutor,
        [SchemaService] ITransactionScopeHandler transactionScopeHandler)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _contextFactory = contextFactory ??
            throw new ArgumentNullException(nameof(contextFactory));
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
            if (IsOperationAllowed(context.Operation, context.Request))
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
            // since the request context is pooled we need to clone the context for
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
            var operationContextOwner = _contextFactory.Create();
            var operationContext = operationContextOwner.OperationContext;

            try
            {
                await ExecuteQueryOrMutationAsync(
                        context,
                        batchDispatcher,
                        operation,
                        operationContext)
                    .ConfigureAwait(false);

                if (operationContext.DeferredScheduler.HasResults &&
                    context.Result is IQueryResult result)
                {
                    var results = operationContext.DeferredScheduler.CreateResultStream(result);
                    var responseStream = new ResponseStream(
                        () => results,
                        ExecutionResultKind.DeferredResult);
                    responseStream.RegisterForCleanup(result);
                    responseStream.RegisterForCleanup(operationContextOwner);
                    context.Result = responseStream;
                    operationContextOwner = null;
                }

                await _next(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // if an operation is canceled we will abandon the the rented operation context
                // to ensure that that abandoned tasks to not leak execution into new operations.
                operationContextOwner = null;
            }
            finally
            {
                operationContextOwner?.Dispose();
            }
        }
    }

    private async Task ExecuteQueryOrMutationAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
        OperationContext operationContext)
    {
        if (operation.Definition.Operation is OperationType.Query)
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
        else if (operation.Definition.Operation is OperationType.Mutation)
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

    private object? GetQueryRootValue(IRequestContext context)
        => RootValueResolver.Resolve(
            context,
            context.Services,
            context.Schema.QueryType,
            ref _cachedQuery);

    private object? GetMutationRootValue(IRequestContext context)
        => RootValueResolver.Resolve(
            context,
            context.Services,
            context.Schema.MutationType!,
            ref _cachedMutation);

    private static bool IsOperationAllowed(IOperation operation, IQueryRequest request)
    {
        if (request.Flags is AllowAll)
        {
            return true;
        }

        var allowed = operation.Definition.Operation switch
        {
            OperationType.Query => (request.Flags & AllowQuery) == AllowQuery,
            OperationType.Mutation => (request.Flags & AllowMutation) == AllowMutation,
            OperationType.Subscription => (request.Flags & AllowSubscription) == AllowSubscription,
            _ => true
        };

        if (allowed && operation.HasIncrementalParts)
        {
            return allowed && (request.Flags & AllowStreams) == AllowStreams;
        }

        return allowed;
    }
}
