using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FederatedQueryExecutor _executor;
    private readonly ISchema _schema;
    private readonly ObjectPool<OperationContext> _operationContextPool;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        ObjectPool<OperationContext> operationContextPool,
        [SchemaService] FederatedQueryExecutor executor,
        [SchemaService] ISchema schema)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _operationContextPool = operationContextPool ??
            throw new ArgumentNullException(nameof(operationContextPool));
        _executor = executor ??
            throw new ArgumentNullException(nameof(executor));
        _schema = schema ??
            throw new ArgumentNullException(nameof(schema));
    }

    public async ValueTask InvokeAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher)
    {
        if (context.Operation is not null &&
            context.Variables is not null &&
            context.Operation.ContextData.TryGetValue(PipelineProps.QueryPlan, out var value) &&
            value is QueryPlan queryPlan)
        {
            var operationContext = _operationContextPool.Get();

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                context.Operation,
                context.Variables,
                new object(), // todo: we can use static representations for these
                () => new object());  // todo: we can use static representations for these

            var federatedQueryContext = new FederatedQueryContext(
                operationContext,
                queryPlan,
                new HashSet<ISelectionSet>(
                    queryPlan.ExecutionNodes
                        .OfType<RequestNode>()
                        .Select(t => t.Handler.SelectionSet)));

            // TODO : just for debug
            if (context.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan))
            {
                var subGraphRequests = new OrderedDictionary<string, object?>();
                var plan = new OrderedDictionary<string, object?>();
                plan.Add("userRequest", context.Document?.ToString());
                plan.Add("subGraphRequests", subGraphRequests);

                var index = 0;
                foreach (var executionNode in queryPlan.ExecutionNodes)
                {
                    if (executionNode is RequestNode rn)
                    {
                        subGraphRequests.Add(
                            $"subGraphRequest{++index}",
                            rn.Handler.Document.ToString());
                    }
                }

                operationContext.Result.SetExtension("queryPlan", plan);
            }

            // we store the context on the result for unit tests.
            operationContext.Result.SetContextData("queryPlan", queryPlan);

            context.Result = await _executor.ExecuteAsync(
                federatedQueryContext,
                context.RequestAborted)
                .ConfigureAwait(false);

            _operationContextPool.Return(operationContext);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }
}
