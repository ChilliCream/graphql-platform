using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FederatedQueryExecutor _executor;
    private readonly ISchema _schema;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        [SchemaService] FederatedQueryExecutor executor,
        [SchemaService] ISchema schema)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _executor = executor ??
            throw new ArgumentNullException(nameof(executor));
        _schema = schema;
    }

    public async ValueTask InvokeAsync(IRequestContext context, ResultBuilder resultBuilder)
    {
        if (context.Operation is not null &&
            context.Variables is not null &&
            context.ContextData.TryGetValue(PipelineProperties.QueryPlan, out var value) &&
            value is QueryPlan queryPlan)
        {
            resultBuilder.Initialize(
                context.Operation,
                context.ErrorHandler,
                context.DiagnosticEvents);

            var federatedQueryContext = new FederatedQueryContext(
                _schema,
                resultBuilder,
                context.Operation,
                queryPlan,
                new HashSet<ISelectionSet>(
                    queryPlan.ExecutionNodes
                        .OfType<RequestNode>()
                        .Select(t => t.Handler.SelectionSet)));

            context.Result = await _executor.ExecuteAsync(
                federatedQueryContext,
                context.RequestAborted)
                .ConfigureAwait(false);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }
}
