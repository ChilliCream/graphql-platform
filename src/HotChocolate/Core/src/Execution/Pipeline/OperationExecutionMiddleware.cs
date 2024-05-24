using System;
using System.Collections.Generic;
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

    private OperationExecutionMiddleware(
        RequestDelegate next,
        IFactory<OperationContextOwner> contextFactory,
        [SchemaService] QueryExecutor queryExecutor,
        [SchemaService] SubscriptionExecutor subscriptionExecutor,
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
            if (!IsOperationAllowed(context.Operation, context.Request))
            {
                context.Result = ErrorHelper.OperationKindNotAllowed();
                return;
            }

            if (!IsRequestTypeAllowed(context.Operation, context.Variables))
            {
                context.Result = ErrorHelper.RequestTypeNotAllowed();
                return;
            }

            using (context.DiagnosticEvents.ExecuteOperation(context))
            {
                if ((context.Variables?.Count ?? 0) is 0 or 1)
                {
                    await ExecuteOperationRequestAsync(context, batchDispatcher, context.Operation)
                        .ConfigureAwait(false);
                }
                else
                {
                    await ExecuteVariableBatchRequestAsync(context, batchDispatcher, context.Operation)
                        .ConfigureAwait(false);
                }
            }
            
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }

    private async Task ExecuteOperationRequestAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation)
    {
        if (operation.Definition.Operation is OperationType.Subscription)
        {
            // since the request context is pooled we need to clone the context for
            // long running executions.
            var cloned = context.Clone();

            context.Result = await _subscriptionExecutor
                .ExecuteAsync(cloned, () => GetQueryRootValue(cloned))
                .ConfigureAwait(false);
        }
        else
        {
            context.Result = 
                await ExecuteQueryOrMutationAsync(
                        context, 
                        batchDispatcher, 
                        operation, 
                        context.Variables![0])
                    .ConfigureAwait(false);
        }
    }

    private async Task ExecuteVariableBatchRequestAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation)
    {
        var variableSet = context.Variables!;
        var variableSetCount = variableSet.Count;
        var tasks = new Task<IOperationResult>[variableSetCount];

        for (var i = 0; i < variableSetCount; i++)
        {
            tasks[i] = ExecuteQueryOrMutationNoStreamAsync(context, batchDispatcher, operation, variableSet[i], i);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        context.Result = new OperationResultBatch(results);
    }

    private async Task<IExecutionResult> ExecuteQueryOrMutationAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
        IVariableValueCollection variables)
    {
        var operationContextOwner = _contextFactory.Create();
        var operationContext = operationContextOwner.OperationContext;

        try
        {
            var result = 
                await ExecuteQueryOrMutationAsync(
                    context,
                    batchDispatcher,
                    operation,
                    operationContext,
                    variables)
                    .ConfigureAwait(false);

            if (operationContext.DeferredScheduler.HasResults)
            {
                var results = operationContext.DeferredScheduler.CreateResultStream(result);
                var responseStream = new ResponseStream(() => results, ExecutionResultKind.DeferredResult);
                responseStream.RegisterForCleanup(result);
                responseStream.RegisterForCleanup(operationContextOwner);
                operationContextOwner = null;
                return responseStream;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            // if an operation is canceled we will abandon the the rented operation context
            // to ensure that that abandoned tasks to not leak execution into new operations.
            operationContextOwner = null;

            // we rethrow so that another middleware can deal with the cancellation.
            throw;
        }
        finally
        {
            operationContextOwner?.Dispose();
        }
    }
    
    private async Task<IOperationResult> ExecuteQueryOrMutationNoStreamAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
        IVariableValueCollection variables,
        int? variableIndex = null)
    {
        var operationContextOwner = _contextFactory.Create();
        var operationContext = operationContextOwner.OperationContext;

        try
        {
            return await ExecuteQueryOrMutationAsync(
                context,
                batchDispatcher,
                operation,
                operationContext,
                variables,
                variableIndex)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // if an operation is canceled we will abandon the the rented operation context
            // to ensure that that abandoned tasks to not leak execution into new operations.
            operationContextOwner = null;

            // we rethrow so that another middleware can deal with the cancellation.
            throw;
        }
        finally
        {
            operationContextOwner?.Dispose();
        }
    }

    private async Task<IOperationResult> ExecuteQueryOrMutationAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
        OperationContext operationContext,
        IVariableValueCollection variables,
        int? variableIndex = null)
    {
        if (operation.Definition.Operation is OperationType.Query)
        {
            var query = GetQueryRootValue(context);

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                operation,
                variables,
                query,
                () => query,
                variableIndex);

            return await _queryExecutor.ExecuteAsync(operationContext).ConfigureAwait(false);
        }

        if (operation.Definition.Operation is OperationType.Mutation)
        {
            using var transactionScope = _transactionScopeHandler.Create(context);

            var mutation = GetMutationRootValue(context);

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                operation,
                variables,
                mutation,
                () => GetQueryRootValue(context),
                variableIndex);

            var result = await _queryExecutor.ExecuteAsync(operationContext).ConfigureAwait(false);
            
            // we capture the result here so that we can capture it in the transaction scope.
            context.Result = result;
            
            // we complete the transaction scope and are done.
            transactionScope.Complete();
            return result;
        }

        throw new InvalidOperationException();
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

    private static bool IsOperationAllowed(IOperation operation, IOperationRequest request)
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
            _ => true,
        };

        if (allowed && operation.HasIncrementalParts)
        {
            return allowed && (request.Flags & AllowStreams) == AllowStreams;
        }

        return allowed;
    }

    private static bool IsRequestTypeAllowed(
        IOperation operation,
        IReadOnlyList<IVariableValueCollection>? variables)
    {
        if (variables is { Count: > 1, })
        {
            return operation.Definition.Operation is not OperationType.Subscription &&
                !operation.HasIncrementalParts;
        }

        return true;
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var contextFactory = core.Services.GetRequiredService<IFactory<OperationContextOwner>>();
            var queryExecutor = core.SchemaServices.GetRequiredService<QueryExecutor>();
            var subscriptionExecutor = core.SchemaServices.GetRequiredService<SubscriptionExecutor>();
            var transactionScopeHandler = core.SchemaServices.GetRequiredService<ITransactionScopeHandler>();
            var middleware = new OperationExecutionMiddleware(
                next,
                contextFactory,
                queryExecutor,
                subscriptionExecutor,
                transactionScopeHandler);

            return async context =>
            {
                var batchDispatcher = context.Services.GetService<IBatchDispatcher>();
                await middleware.InvokeAsync(context, batchDispatcher).ConfigureAwait(false);
            };
        };
}