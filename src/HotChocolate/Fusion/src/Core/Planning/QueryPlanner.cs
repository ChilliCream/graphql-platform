using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanner
{
    private readonly QueryPlanDelegate _pipeline;

    public QueryPlanner(FusionGraphConfiguration configuration, ISchema schema)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _pipeline =
            QueryPlanPipelineBuilder
                .New()
                .Use(() => new ExecutionStepDiscoveryMiddleware(configuration))
                .Use<RequirementsPlannerMiddleware>()
                .Use(() => new ExecutionNodeBuilderMiddleware(configuration, schema))
                .Use(() => new ExecutionTreeBuilderMiddleware(schema))
                .Build();
    }

    public QueryPlan Plan(IOperation operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var queryPlanContext = new QueryPlanContext(operation);
        _pipeline(queryPlanContext);

        if (queryPlanContext.Plan is null)
        {
            throw new InvalidOperationException();
        }

        return queryPlanContext.Plan;
    }
}
