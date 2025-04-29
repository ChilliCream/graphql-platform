using HotChocolate.Fusion.Planning;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class FusionGraphQLRequestContextExtensions
{
    public static ExecutionPlan? GetExecutionPlan(
        this GraphQLRequestContext context)
        => context.Features.Get<ExecutionPlan>();

    public static void SetExecutionPlan(
        this GraphQLRequestContext context,
        ExecutionPlan plan)
        => context.Features.Set(plan);
}
