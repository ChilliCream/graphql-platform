using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

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
    /// The <see cref="OperationPlan"/> if it exists, otherwise <c>null</c>.
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
    /// Gets the <see cref="OperationPlan"/> from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The <see cref="OperationPlan"/> if it exists, otherwise <c>null</c>.
    /// </returns>
    public static OperationPlan? GetOperationPlan(
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
    /// Sets the <see cref="OperationPlan"/> on the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="plan">
    /// The operation execution plan.
    /// </param>
    public static void SetOperationPlan(
        this RequestContext context,
        OperationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plan);

        context.Features.GetOrSet<FusionOperationInfo>().OperationPlan = plan;
    }

    internal static ResultPoolSessionHolder CreateResultPoolSession(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var objectPool = context.RequestServices.GetRequiredService<ObjectPool<ResultPoolSession>>();
        return new ResultPoolSessionHolder(objectPool);
    }

    internal static bool CollectOperationPlanTelemetry(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Schema.GetRequestOptions().CollectOperationPlanTelemetry;
    }

    internal static ErrorHandlingMode ErrorHandlingMode(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestOptions = context.Schema.GetRequestOptions();

        if (context.Request.ErrorHandlingMode is { } errorHandlingMode
            && requestOptions.AllowErrorHandlingModeOverride)
        {
            return errorHandlingMode;
        }

        return requestOptions.DefaultErrorHandlingMode;
    }

    internal static bool AllowErrorHandlingModeOverride(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Schema.GetRequestOptions().AllowErrorHandlingModeOverride;
    }

    internal static ISourceSchemaClientScope CreateClientScope(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var clientScopeFactory = context.RequestServices.GetRequiredService<ISourceSchemaClientScopeFactory>();
        return clientScopeFactory.CreateScope(context.Schema);
    }
}
