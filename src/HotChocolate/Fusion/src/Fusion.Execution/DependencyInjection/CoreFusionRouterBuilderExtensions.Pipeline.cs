using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds document caching to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseDocumentCache(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseDocumentCache(builder);
        return builder;
    }

    /// <summary>
    /// Adds document parsing to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseDocumentParser(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseDocumentParser(builder);
        return builder;
    }

    /// <summary>
    /// Adds document validation to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseDocumentValidation(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseDocumentValidation(builder);
        return builder;
    }

    /// <summary>
    /// Adds unhandled exception handling to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseExceptions(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseExceptions(builder);
        return builder;
    }

    /// <summary>
    /// Adds timeout handling to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseTimeout(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseTimeout(builder);
        return builder;
    }

    /// <summary>
    /// Adds instrumentation to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseInstrumentation(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseInstrumentation(builder);
        return builder;
    }

    /// <summary>
    /// Adds operation plan caching to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseOperationPlanCache(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseOperationPlanCache(builder);
        return builder;
    }

    /// <summary>
    /// Adds operation planning to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseOperationPlan(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseOperationPlan(builder);
        return builder;
    }

    /// <summary>
    /// Adds the concurrency gate to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseConcurrencyGate(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseConcurrencyGate(builder);
        return builder;
    }

    /// <summary>
    /// Adds operation execution to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseOperationExecution(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseOperationExecution(builder);
        return builder;
    }

    /// <summary>
    /// Adds operation variable coercion to the request pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseOperationVariableCoercion(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseOperationVariableCoercion(builder);
        return builder;
    }

    /// <summary>
    /// Adds middleware that skips execution for warmup requests.
    /// </summary>
    public static IFusionRouterBuilder UseSkipWarmupExecution(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseSkipWarmupExecution(builder);
        return builder;
    }

    /// <summary>
    /// Adds middleware that reads persisted operations.
    /// </summary>
    public static IFusionRouterBuilder UseReadPersistedOperation(
        this IFusionRouterBuilder builder,
        string? after = null,
        string? before = null)
    {
        CoreFusionGatewayBuilderExtensions.UseReadPersistedOperation(builder, after, before);
        return builder;
    }

    /// <summary>
    /// Adds not-found handling for automatic persisted operations.
    /// </summary>
    public static IFusionRouterBuilder UseAutomaticPersistedOperationNotFound(
        this IFusionRouterBuilder builder,
        string? after = null,
        string? before = null)
    {
        CoreFusionGatewayBuilderExtensions.UseAutomaticPersistedOperationNotFound(builder, after, before);
        return builder;
    }

    /// <summary>
    /// Adds middleware that writes persisted operations.
    /// </summary>
    public static IFusionRouterBuilder UseWritePersistedOperation(
        this IFusionRouterBuilder builder,
        string? after = null,
        string? before = null)
    {
        CoreFusionGatewayBuilderExtensions.UseWritePersistedOperation(builder, after, before);
        return builder;
    }

    /// <summary>
    /// Adds not-found handling for persisted operations.
    /// </summary>
    public static IFusionRouterBuilder UsePersistedOperationNotFound(
        this IFusionRouterBuilder builder,
        string? after = null,
        string? before = null)
    {
        CoreFusionGatewayBuilderExtensions.UsePersistedOperationNotFound(builder, after, before);
        return builder;
    }

    /// <summary>
    /// Adds middleware that enforces the persisted-operations-only policy.
    /// </summary>
    public static IFusionRouterBuilder UseOnlyPersistedOperationAllowed(
        this IFusionRouterBuilder builder,
        string? after = null,
        string? before = null)
    {
        CoreFusionGatewayBuilderExtensions.UseOnlyPersistedOperationAllowed(builder, after, before);
        return builder;
    }

    /// <summary>
    /// Replaces the request pipeline with the default router pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseDefaultPipeline(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseDefaultPipeline(builder);
        return builder;
    }

    /// <summary>
    /// Replaces the request pipeline with the persisted operation pipeline.
    /// </summary>
    public static IFusionRouterBuilder UsePersistedOperationPipeline(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UsePersistedOperationPipeline(builder);
        return builder;
    }

    /// <summary>
    /// Replaces the request pipeline with the automatic persisted operation pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseAutomaticPersistedOperationPipeline(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.UseAutomaticPersistedOperationPipeline(builder);
        return builder;
    }
}
