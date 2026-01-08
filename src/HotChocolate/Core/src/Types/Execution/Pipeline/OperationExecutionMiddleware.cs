using System.Buffers;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.RequestFlags;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFactory<OperationContextOwner> _contextFactory;
    private readonly QueryExecutor _queryExecutor;
    private readonly SubscriptionExecutor _subscriptionExecutor;
    private readonly ITransactionScopeHandler _transactionScopeHandler;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private object? _cachedQuery;
    private object? _cachedMutation;

    private OperationExecutionMiddleware(
        RequestDelegate next,
        IFactory<OperationContextOwner> contextFactory,
        QueryExecutor queryExecutor,
        SubscriptionExecutor subscriptionExecutor,
        ITransactionScopeHandler transactionScopeHandler,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(subscriptionExecutor);
        ArgumentNullException.ThrowIfNull(transactionScopeHandler);

        _next = next;
        _contextFactory = contextFactory;
        _queryExecutor = queryExecutor;
        _subscriptionExecutor = subscriptionExecutor;
        _transactionScopeHandler = transactionScopeHandler;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(
        RequestContext context,
        IBatchDispatcher? batchDispatcher)
    {
        if (batchDispatcher is null)
        {
            throw OperationExecutionMiddleware_NoBatchDispatcher();
        }

        if (context.TryGetOperation(out var operation) && context.VariableValues.Length > 0)
        {
            if (!IsOperationAllowed(operation, context.Request))
            {
                context.Result = ErrorHelper.OperationKindNotAllowed();
                return;
            }

            if (!IsRequestTypeAllowed(operation, context.VariableValues))
            {
                context.Result = ErrorHelper.RequestTypeNotAllowed();
                return;
            }

            using (_diagnosticEvents.ExecuteOperation(context))
            {
                if (context.VariableValues.Length is 0 or 1)
                {
                    await ExecuteOperationRequestAsync(context, batchDispatcher, operation).ConfigureAwait(false);
                }
                else
                {
                    await ExecuteVariableBatchRequestAsync(context, batchDispatcher, operation).ConfigureAwait(false);
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
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation)
    {
        if (operation.Definition.Operation is OperationType.Subscription)
        {
            context.Result = await _subscriptionExecutor
                .ExecuteAsync(context, () => GetQueryRootValue(context))
                .ConfigureAwait(false);
        }
        else
        {
            context.Result =
                await ExecuteQueryOrMutationAsync(
                        context,
                        batchDispatcher,
                        operation,
                        context.VariableValues[0])
                    .ConfigureAwait(false);
        }
    }

    private async Task ExecuteVariableBatchRequestAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation)
    {
        if (operation.Definition.Operation is OperationType.Query)
        {
            await ExecuteVariableBatchRequestOptimizedAsync(context, batchDispatcher, operation);
            return;
        }

        var variableSet = context.VariableValues;
        var tasks = new Task<OperationResult>[variableSet.Length];

        for (var i = 0; i < variableSet.Length; i++)
        {
            tasks[i] = ExecuteQueryOrMutationNoStreamAsync(context, batchDispatcher, operation, variableSet[i], i);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        context.Result = new OperationResultBatch([.. results]);
    }

    private async Task ExecuteVariableBatchRequestOptimizedAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation)
    {
        var variableSets = context.VariableValues;
        var query = GetQueryRootValue(context);
        var operationContextBuffer = ArrayPool<OperationContextOwner>.Shared.Rent(variableSets.Length);
        var resultBuffer = ArrayPool<IExecutionResult>.Shared.Rent(variableSets.Length);

        for (var variableIndex = 0; variableIndex < variableSets.Length; variableIndex++)
        {
            Initialize(
                context,
                batchDispatcher,
                operation,
                query,
                operationContextBuffer.AsSpan(0, variableSets.Length),
                variableSets[variableIndex],
                variableIndex,
                _contextFactory);
        }

        try
        {
            await _queryExecutor.ExecuteBatchAsync(
                operationContextBuffer.AsMemory(0, variableSets.Length),
                resultBuffer.AsMemory(0, variableSets.Length));

            context.Result = new OperationResultBatch([.. resultBuffer.AsSpan(0, variableSets.Length)]);
        }
        catch (OperationCanceledException)
        {
            // if an operation is canceled we will abandon the rented operation contexts
            // to ensure that that abandoned tasks do not leak into new operations.
            AbandonContexts(ref operationContextBuffer, variableSets.Length);

            // we rethrow so that another middleware can deal with the cancellation.
            throw;
        }
        finally
        {
            ReleaseResources(ref operationContextBuffer, resultBuffer, variableSets.Length);
        }

        static void Initialize(
            RequestContext context,
            IBatchDispatcher batchDispatcher,
            Operation operation,
            object? query,
            Span<OperationContextOwner> operationContexts,
            IVariableValueCollection variables,
            int variableIndex,
            IFactory<OperationContextOwner> operationContextFactory)
        {
            var operationContextOwner = operationContextFactory.Create();
            var operationContext = operationContextOwner.OperationContext;

            operationContext.Initialize(
                context,
                context.RequestServices,
                batchDispatcher,
                operation,
                variables,
                query,
                () => query,
                variableIndex);

            operationContexts[variableIndex] = operationContextOwner;
        }

        static void AbandonContexts(ref OperationContextOwner[]? operationContextBuffer, int length)
        {
            if (operationContextBuffer is not null)
            {
                operationContextBuffer.AsSpan(0, length).Clear();
                ArrayPool<OperationContextOwner>.Shared.Return(operationContextBuffer);
            }

            operationContextBuffer = null;
        }

        static void ReleaseResources(
            ref OperationContextOwner[]? operationContextBuffer,
            IExecutionResult[] resultBuffer,
            int length)
        {
            var results = resultBuffer.AsSpan(0, length);
            results.Clear();
            ArrayPool<IExecutionResult>.Shared.Return(resultBuffer);

            if (operationContextBuffer is null)
            {
                return;
            }

            var contextOwners = operationContextBuffer.AsSpan(0, length);

            foreach (var contextOwner in contextOwners)
            {
                contextOwner.Dispose();
            }

            contextOwners.Clear();

            ArrayPool<OperationContextOwner>.Shared.Return(operationContextBuffer);
        }
    }

    private async Task<IExecutionResult> ExecuteQueryOrMutationAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation,
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

            // TODO : DEFER
            // if (operationContext.DeferredScheduler.HasResults)
            // {
            //    var results = operationContext.DeferredScheduler.CreateResultStream(result);
            //    var responseStream = new ResponseStream(() => results, ExecutionResultKind.DeferredResult);
            //    responseStream.RegisterForCleanup(result);
            //    responseStream.RegisterForCleanup(operationContextOwner);
            //    operationContextOwner = null;
            //    return responseStream;
            // }

            return result;
        }
        catch (OperationCanceledException)
        {
            // if an operation is canceled we will abandon the rented operation context
            // to ensure that that abandoned tasks do not leak into new operations.
            operationContextOwner = null;

            // we rethrow so that another middleware can deal with the cancellation.
            throw;
        }
        finally
        {
            operationContextOwner?.Dispose();
        }
    }

    private async Task<OperationResult> ExecuteQueryOrMutationNoStreamAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation,
        IVariableValueCollection variables,
        int variableIndex)
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
            // if an operation is canceled we will abandon the rented operation context
            // to ensure that the abandoned tasks do not leak into new operations.
            operationContextOwner = null;

            // we rethrow so that another middleware can deal with the cancellation.
            throw;
        }
        finally
        {
            operationContextOwner?.Dispose();
        }
    }

    private async Task<OperationResult> ExecuteQueryOrMutationAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation,
        OperationContext operationContext,
        IVariableValueCollection variables,
        int variableIndex = -1)
    {
        if (operation.Definition.Operation is OperationType.Query)
        {
            var query = GetQueryRootValue(context);

            operationContext.Initialize(
                context,
                context.RequestServices,
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
                context.RequestServices,
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

    private object? GetQueryRootValue(RequestContext context)
        => RootValueResolver.Resolve(
            context,
            context.RequestServices,
            Unsafe.As<ObjectType>(context.Schema.QueryType),
            ref _cachedQuery);

    private object? GetMutationRootValue(RequestContext context)
        => RootValueResolver.Resolve(
            context,
            context.RequestServices,
            Unsafe.As<ObjectType>(context.Schema.MutationType)!,
            ref _cachedMutation);

    private static bool IsOperationAllowed(Operation operation, IOperationRequest request)
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

        // TODO : DEFER
        // if (allowed && operation.HasIncrementalParts)
        // {
        //    return allowed && (request.Flags & AllowStreams) == AllowStreams;
        // }

        return allowed;
    }

    private static bool IsRequestTypeAllowed(
        Operation operation,
        IReadOnlyList<IVariableValueCollection>? variables)
    {
        if (variables is { Count: > 1 })
        {
            // TODO : DEFER
            return operation.Definition.Operation is not OperationType.Subscription;
            // && !operation.HasIncrementalParts;
        }

        return true;
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (factoryContext, next) =>
            {
                var contextFactory = factoryContext.Services.GetRequiredService<IFactory<OperationContextOwner>>();
                var queryExecutor = factoryContext.SchemaServices.GetRequiredService<QueryExecutor>();
                var subscriptionExecutor = factoryContext.SchemaServices.GetRequiredService<SubscriptionExecutor>();
                var transactionScopeHandler =
                    factoryContext.SchemaServices.GetRequiredService<ITransactionScopeHandler>();
                var diagnosticEvents = factoryContext.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var middleware = new OperationExecutionMiddleware(
                    next,
                    contextFactory,
                    queryExecutor,
                    subscriptionExecutor,
                    transactionScopeHandler,
                    diagnosticEvents);

                return async context =>
                {
                    var batchDispatcher = context.RequestServices.GetService<IBatchDispatcher>();
                    await middleware.InvokeAsync(context, batchDispatcher).ConfigureAwait(false);
                };
            },
            WellKnownRequestMiddleware.OperationExecutionMiddleware);
}
