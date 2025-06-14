using HotChocolate.Fusion.Execution.Nodes;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class FusionRequestContextExtensions
{
    public static OperationExecutionPlan? GetOperationExecutionPlan(
        this RequestContext context)
        => context.Features.Get<OperationExecutionPlan>();

    public static void SetOperationExecutionPlan(
        this RequestContext context,
        OperationExecutionPlan plan)
        => context.Features.Set(plan);
}
