using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationQueryPlanCompiler(QueryPlanner queryPlanner) : IOperationOptimizer
{
    private readonly QueryPlanner _queryPlanner = queryPlanner
        ?? throw new ArgumentNullException(nameof(queryPlanner));

    public void OptimizeOperation(OperationOptimizerContext context)
    {
        var operation = context.CreateOperation();
        var queryPlan = _queryPlanner.Plan(operation);
        context.ContextData[PipelineProps.QueryPlan] = queryPlan;
    }
}
