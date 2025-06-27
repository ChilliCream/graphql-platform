using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
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
    /// Gets the operation id.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The <see cref="OperationExecutionPlan"/> if it exists, otherwise <c>null</c>.
    /// </returns>
    public static string GetOperationId(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationId = context.Features.Get<FusionOperationInfo>()?.OperationId;

        if (string.IsNullOrEmpty(operationId))
        {
            throw new InvalidOperationException("The operation identifier was not set.");
        }

        return operationId;
    }

    /// <summary>
    /// Gets the <see cref="OperationExecutionPlan"/> from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The <see cref="OperationExecutionPlan"/> if it exists, otherwise <c>null</c>.
    /// </returns>
    public static OperationExecutionPlan? GetOperationPlan(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.Get<FusionOperationInfo>()?.OperationPlan;
    }

    /// <summary>
    /// Sets the operation identifier.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="id">
    /// The operation id.
    /// </param>
    public static void SetOperationId(
        this RequestContext context,
        string id)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(id);

        context.Features.GetOrSet<FusionOperationInfo>().OperationId = id;
    }

    /// <summary>
    /// Sets the <see cref="OperationExecutionPlan"/> on the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="plan">
    /// The operation execution plan.
    /// </param>
    public static void SetOperationPlan(
        this RequestContext context,
        OperationExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plan);

        context.Features.GetOrSet<FusionOperationInfo>().OperationPlan = plan;
    }
}
