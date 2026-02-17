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
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private object? _cachedQuery;
    private object? _cachedMutation;

    private OperationExecutionMiddleware(
        RequestDelegate next,
        IFactory<OperationContextOwner> contextFactory,
        QueryExecutor queryExecutor,
        SubscriptionExecutor subscriptionExecutor,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(subscriptionExecutor);

        _next = next;
        _contextFactory = contextFactory;
        _queryExecutor = queryExecutor;
        _subscriptionExecutor = subscriptionExecutor;
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
                if (operation.Definition.Operation is OperationType.Subscription)
                {
                    context.Result = await _subscriptionExecutor
                        .ExecuteAsync(context, () => GetQueryRootValue(context))
                        .ConfigureAwait(false);
                }
                else if (context.VariableValues.Length is 0 or 1)
                {
                    context.Result =
                        await ExecuteQueryOrMutationAsync(
                                context,
                                batchDispatcher,
                                operation,
                                context.VariableValues[0])
                            .ConfigureAwait(false);
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

    private async Task ExecuteVariableBatchRequestAsync(
        RequestContext context,
        IBatchDispatcher batchDispatcher,
        Operation operation)
    {
        var variableSets = context.VariableValues;
        var queryRoot = GetQueryRootValue(context);
        var rootValue = operation.Definition.Operation is OperationType.Mutation
            ? GetMutationRootValue(context)
            : queryRoot;
        var operationContextBuffer = ArrayPool<OperationContextOwner>.Shared.Rent(variableSets.Length);
        var resultBuffer = ArrayPool<IExecutionResult>.Shared.Rent(variableSets.Length);

        for (var variableIndex = 0; variableIndex < variableSets.Length; variableIndex++)
        {
            Initialize(
                context,
                batchDispatcher,
                operation,
                rootValue,
                queryRoot,
                operationContextBuffer.AsSpan(0, variableSets.Length),
                variableSets[variableIndex],
                variableIndex,
                _contextFactory);
        }

        try
        {
            await _queryExecutor.ExecuteBatchAsync(
                operationContextBuffer,
                resultBuffer,
                variableSets.Length);

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
            object? rootValue,
            object? queryRoot,
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
                rootValue,
                () => queryRoot,
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
            var queryRoot = GetQueryRootValue(context);
            var rootValue = operation.Definition.Operation is OperationType.Mutation
                ? GetMutationRootValue(context)
                : queryRoot;

            operationContext.Initialize(
                context,
                context.RequestServices,
                batchDispatcher,
                operation,
                variables,
                rootValue,
                () => queryRoot);

            var result = await _queryExecutor.ExecuteAsync(operationContext).ConfigureAwait(false);

            if (result.IsStreamResult())
            {
                result.RegisterForCleanup(operationContextOwner);
                operationContextOwner = null;
            }

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

    private object? GetQueryRootValue(RequestContext context)
    {
        var queryType = context.Schema.QueryType;

        if (queryType is null)
        {
            return null;
        }

        return RootValueResolver.Resolve(
            context,
            context.RequestServices,
            Unsafe.As<ObjectType>(queryType),
            ref _cachedQuery);
    }

    private object? GetMutationRootValue(RequestContext context)
    {
        var mutationType = context.Schema.MutationType;

        if (mutationType is null)
        {
            return null;
        }

        return RootValueResolver.Resolve(
            context,
            context.RequestServices,
            Unsafe.As<IObjectTypeDefinition, ObjectType>(ref mutationType),
            ref _cachedMutation);
    }

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

        if (allowed && operation.HasIncrementalParts)
        {
            return (request.Flags & AllowStreams) == AllowStreams;
        }

        return allowed;
    }

    private static bool IsRequestTypeAllowed(
        Operation operation,
        IReadOnlyList<IVariableValueCollection>? variables)
    {
        if (variables is { Count: > 1 })
        {
            return operation.Definition.Operation is not OperationType.Subscription;
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
                var diagnosticEvents = factoryContext.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var middleware = new OperationExecutionMiddleware(
                    next,
                    contextFactory,
                    queryExecutor,
                    subscriptionExecutor,
                    diagnosticEvents);

                return async context =>
                {
                    var batchDispatcher = context.RequestServices.GetService<IBatchDispatcher>();
                    await middleware.InvokeAsync(context, batchDispatcher).ConfigureAwait(false);
                };
            },
            WellKnownRequestMiddleware.OperationExecutionMiddleware);
}
