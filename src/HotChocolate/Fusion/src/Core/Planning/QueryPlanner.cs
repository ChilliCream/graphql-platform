using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning.Pipeline;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlanner
{
    private readonly QueryPlanDelegate _pipeline;

    public QueryPlanner(FusionGraphConfiguration configuration, ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _pipeline =
            QueryPlanPipelineBuilder
                .New()
                .Use(() => new ExecutionStepDiscoveryMiddleware(schema, configuration))
                .Use(() => new FieldRequirementsPlannerMiddleware(configuration))
                .Use<RequirementsPlannerMiddleware>()
                .Use(() => new ExecutionNodeBuilderMiddleware(configuration, schema))
                .Use(() => new ExecutionTreeBuilderMiddleware(schema))
                .Build();
    }

    public QueryPlan Plan(IOperation operation)
    {
        var queryPlanContext = new QueryPlanContext(operation);
        _pipeline(queryPlanContext);

        return queryPlanContext.BuildQueryPlan();
    }
}
