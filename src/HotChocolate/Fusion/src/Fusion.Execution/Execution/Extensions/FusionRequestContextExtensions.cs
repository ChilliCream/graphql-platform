using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Pipeline;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using ExecutionThrowHelper = HotChocolate.Fusion.Execution.ThrowHelper;

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
    /// Gets the unique id for the selected operation and executor version.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The operation id.
    /// </returns>
    public static string GetOperationId(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<FusionOperationInfo>();

        if (operationInfo.OperationId is { } operationId)
        {
            return operationId;
        }

        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.Document is null)
        {
            throw ExecutionThrowHelper.OperationDocumentNotAvailable();
        }

        if (documentInfo.Hash.IsEmpty)
        {
            throw ExecutionThrowHelper.OperationDocumentHashNotAvailable();
        }

        operationId = documentInfo.OperationCount == 1
            ? documentInfo.Hash.Value
            : $"{documentInfo.Hash.Value}.{context.Request.OperationName ?? "Default"}";

        operationInfo.OperationId = operationId;
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
        context.Features.Set<IOperation>(plan.Operation);

        // Release waiting requests as soon as the plan is available, before the leader finishes execution.
        context.Features.Get<OperationPlanInFlightRelease>()?.TryRelease(context, plan);
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

    internal static bool AllowOperationPlanRequests(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<OperationPlanRequestOverrides>()?.IsAllowed == true)
        {
            return true;
        }

        return context.Schema.GetRequestOptions().AllowOperationPlanRequests;
    }

    internal static ISourceSchemaClientScope CreateClientScope(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var clientScopeFactory = context.RequestServices.GetRequiredService<ISourceSchemaClientScopeFactory>();
        return clientScopeFactory.CreateScope(context.Schema);
    }
}
