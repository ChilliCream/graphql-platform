using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class FusionGraphQLRequestContextExtensions
{
    public static OperationPlan? GetExecutionPlan(
        this GraphQLRequestContext context)
        => context.Features.Get<OperationPlan>();

    public static void SetExecutionPlan(
        this GraphQLRequestContext context,
        OperationPlan plan)
        => context.Features.Set(plan);
}
