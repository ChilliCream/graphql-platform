using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class OperationQueryPlanCompiler : IOperationOptimizer
{
    private readonly RequestPlanner _requestPlanner;
    private readonly RequirementsPlanner _requirementsPlanner;
    private readonly ExecutionPlanBuilder _executionPlanBuilder;

    public OperationQueryPlanCompiler(
        RequestPlanner requestPlanner,
        RequirementsPlanner requirementsPlanner,
        ExecutionPlanBuilder executionPlanBuilder)
    {
        _requestPlanner = requestPlanner;
        _requirementsPlanner = requirementsPlanner;
        _executionPlanBuilder = executionPlanBuilder;
    }

    public void OptimizeOperation(OperationOptimizerContext context)
    {
        var temporaryOperation = context.CreateOperation();
        var queryPlanContext = new QueryPlanContext(temporaryOperation);
        _requestPlanner.Plan(queryPlanContext);
        _requirementsPlanner.Plan(queryPlanContext);
        var queryPlan = _executionPlanBuilder.Build(queryPlanContext);
        context.ContextData[PipelineProps.QueryPlan] = queryPlan;
    }
}
