using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Pipeline;

/// <summary>
/// Creates the query plan for the federated request.
/// </summary>
internal sealed class QueryPlanMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestPlaner _requestPlaner;
    private readonly RequirementsPlaner _requirementsPlaner;
    private readonly ExecutionPlanBuilder _executionPlanBuilder;

    public QueryPlanMiddleware(
        RequestDelegate next,
        [SchemaService] RequestPlaner requestPlaner,
        [SchemaService] RequirementsPlaner requirementsPlaner,
        [SchemaService] ExecutionPlanBuilder executionPlanBuilder)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _requestPlaner = requestPlaner ??
            throw new ArgumentNullException(nameof(requestPlaner));
        _requirementsPlaner = requirementsPlaner ??
            throw new ArgumentNullException(nameof(requirementsPlaner));
        _executionPlanBuilder = executionPlanBuilder ??
            throw new ArgumentNullException(nameof(executionPlanBuilder));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Operation is not null && context.Variables is not null)
        {
            var queryPlanContext = new QueryPlanContext(context.Operation);
            _requestPlaner.Plan(queryPlanContext);
            _requirementsPlaner.Plan(queryPlanContext);
            var queryPlan = _executionPlanBuilder.Build(queryPlanContext);
            context.ContextData[PipelineProperties.QueryPlan] = queryPlan;
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }
}
