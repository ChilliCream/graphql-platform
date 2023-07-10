using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class OperationQueryPlanCompiler : IOperationOptimizer
{
    private readonly QueryPlanner _queryPlanner;

    public OperationQueryPlanCompiler(QueryPlanner queryPlanner)
    {
        _queryPlanner = queryPlanner ??
            throw new ArgumentNullException(nameof(queryPlanner));
    }

    public void OptimizeOperation(OperationOptimizerContext context)
    {
        var operation = context.CreateOperation();
        var queryPlan = _queryPlanner.Plan(operation);
        context.ContextData.Set(PipelineProps.QueryPlan, queryPlan);
    }
}
