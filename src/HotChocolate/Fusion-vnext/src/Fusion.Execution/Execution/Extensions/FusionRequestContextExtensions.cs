using HotChocolate.Fusion.Execution.Nodes;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class FusionRequestContextExtensions
{
    public static OperationPlan? GetExecutionPlan(
        this RequestContext context)
        => context.Features.Get<OperationPlan>();

    public static void SetExecutionPlan(
        this RequestContext context,
        OperationPlan plan)
        => context.Features.Set(plan);
}
