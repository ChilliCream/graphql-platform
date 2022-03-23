using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Types;
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
    private readonly IQueryPlanCache _queryPlanCache;
    private readonly ITransactionScopeHandler _transactionScopeHandler;
    private object? _cachedQuery;
    private object? _cachedMutation;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        ObjectPool<OperationContext> operationContextPool,
        QueryExecutor queryExecutor,
        SubscriptionExecutor subscriptionExecutor,
        IQueryPlanCache queryPlanCache,
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
        _queryPlanCache = queryPlanCache ??
            throw new ArgumentNullException(nameof(queryPlanCache));
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
                QueryPlan queryPlan = GetQueryPlan(context);

                using (context.DiagnosticEvents.ExecuteOperation(context))
                {
                    await ExecuteOperationAsync(
                        context,
                        batchDispatcher,
                        context.Operation,
                        queryPlan)
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
        IPreparedOperation operation,
        QueryPlan queryPlan)
    {
        if (operation.Definition.Operation == OperationType.Subscription)
        {
            // since the context is pooled we need to clone the context for
            // long running executions.
            IRequestContext clonedContext = context.Clone();

            DefaultRequestContextAccessor accessor =
                clonedContext.Services.GetRequiredService<DefaultRequestContextAccessor>();
            accessor.RequestContext = clonedContext;

            // This prevents a closure from being formed over the clonedContext making the schema long lived.
            IQueryRequest request = clonedContext.Request;
            IActivator activator = clonedContext.Activator;
            IServiceProvider services = clonedContext.Services;
            ObjectType queryType = clonedContext.Schema.QueryType;

            context.Result = await _subscriptionExecutor
                .ExecuteAsync(clonedContext, queryPlan, () => RootValueResolver.Resolve(
                    request,
                    activator,
                    services,
                    queryType,
                    ref _cachedQuery))
                .ConfigureAwait(false);

            await _next(clonedContext).ConfigureAwait(false);
        }
        else
        {
            OperationContext? operationContext = _operationContextPool.Get();

            try
            {
                await ExecuteQueryOrMutationAsync(
                    context, batchDispatcher, operation, queryPlan, operationContext)
                    .ConfigureAwait(false);

                if (context.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan) &&
                    context.Result is IQueryResult original)
                {
                    var serializedQueryPlan = new Dictionary<string, object?>
                        {
                            { "flow", QueryPlanBuilder.Prepare(operation).Serialize() },
                            { "selections", operation.Print() }
                        };

                    context.Result = QueryResultBuilder
                        .FromResult(original)
                        .AddExtension("queryPlan", serializedQueryPlan)
                        .Create();
                }

                if (operationContext.Scheduler.DeferredWork.HasWork &&
                    context.Result is IQueryResult result)
                {
                    // if we have deferred query task we will take ownership
                    // of the life time handling and return the operation context
                    // once we handled all deferred tasks.
                    var operationContextOwner = new OperationContextOwner(
                        operationContext, _operationContextPool);

                    var streamSession = new StreamSession(
                        new IDisposable[]
                        {
                            operationContextOwner,

                            // the diagnostic scope needs to be last to be disposed last.
                            context.DiagnosticEvents.ExecuteStream(context)
                        });

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
                    context.Result.RegisterForCleanup(streamSession);
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
        QueryPlan queryPlan,
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
                queryPlan,
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

            var mutation = GetMutationRootValue(context);

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                operation,
                queryPlan,
                context.Variables!,
                mutation,
                () => GetQueryRootValue(context));

            context.Result = await _queryExecutor
                .ExecuteAsync(operationContext)
                .ConfigureAwait(false);

            transactionScope.Complete();
        }
    }

    private QueryPlan GetQueryPlan(IRequestContext context)
    {
        if (!_queryPlanCache.TryGetQueryPlan(context.OperationId!, out QueryPlan? queryPlan))
        {
            queryPlan = QueryPlanBuilder.Build(context.Operation!);
            _queryPlanCache.TryAddQueryPlan(context.OperationId!, queryPlan);
        }

        return queryPlan;
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
        OperationType[]? allowedOps = context.Request.AllowedOperations;

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

    private sealed class StreamSession : IDisposable
    {
        private readonly IDisposable[] _disposables;
        private bool _disposed;

        public StreamSession(IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IDisposable disposable in _disposables)
                {
                    disposable.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
