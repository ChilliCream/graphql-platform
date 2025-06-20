using HotChocolate.Fusion.Execution.Nodes;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HotChocolate.Execution;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extension methods for <see cref="RequestContext"/>.
/// </summary>
public static class FusionRequestContextExtensions
{
    /// <summary>
    /// Gets the <see cref="OperationExecutionPlan"/> from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The <see cref="OperationExecutionPlan"/> if it exists, otherwise <c>null</c>.
    /// </returns>
    public static OperationExecutionPlan? GetOperationExecutionPlan(
        this RequestContext context)
        => context.Features.Get<OperationExecutionPlan>();

    /// <summary>
    /// Sets the <see cref="OperationExecutionPlan"/> on the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="plan">
    /// The operation execution plan.
    /// </param>
    public static void SetOperationExecutionPlan(
        this RequestContext context,
        OperationExecutionPlan plan)
        => context.Features.Set(plan);
}
