using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

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
        context.Features.Set<IOperation>(plan.Operation);
    }

    /// <summary>
    /// Gets the normalized operation document from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The normalized operation document.
    /// </returns>
    internal static DocumentNode GetNormalizedDocument(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedDocument = context.Features.Get<FusionOperationInfo>()?.NormalizedDocument;

        if (normalizedDocument is null)
        {
            throw new InvalidOperationException("The normalized document was not set.");
        }

        return normalizedDocument;
    }

    /// <summary>
    /// Tries to get the normalized operation definition from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="operation">
    /// The normalized operation definition, if one is available.
    /// </param>
    /// <returns>
    /// <c>true</c> if a normalized operation definition is available, otherwise <c>false</c>.
    /// </returns>
    internal static bool TryGetNormalizedOperation(
        this RequestContext context,
        [NotNullWhen(true)] out OperationDefinitionNode? operation)
    {
        ArgumentNullException.ThrowIfNull(context);

        operation = context.Features.Get<FusionOperationInfo>()?.NormalizedOperation;
        return operation is not null;
    }

    /// <summary>
    /// Sets the normalized operation document and its operation definition on the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="document">
    /// The normalized operation document.
    /// </param>
    /// <param name="operation">
    /// The operation definition contained in <paramref name="document"/>.
    /// </param>
    internal static void SetNormalizedDocument(
        this RequestContext context,
        DocumentNode document,
        OperationDefinitionNode operation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);

        var info = context.Features.GetOrSet<FusionOperationInfo>();
        info.NormalizedDocument = document;
        info.NormalizedOperation = operation;
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
