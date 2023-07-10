using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Pipeline;

internal static class PipelineProps
{
    public static readonly StateKey<QueryPlan> QueryPlan = StateKey.Create<QueryPlan>("HotChocolate.Fusion.Pipeline.QueryPlan");
}
