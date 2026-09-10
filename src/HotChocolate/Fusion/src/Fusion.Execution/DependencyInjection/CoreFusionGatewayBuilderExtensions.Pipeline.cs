using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Pipeline;
using static HotChocolate.Fusion.Configuration.FusionSetupUtilities;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    [Obsolete("Use UseDocumentCache on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseDocumentCache(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentCache);
    }

    [Obsolete("Use UseDocumentParser on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseDocumentParser(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentParser);
    }

    [Obsolete("Use UseDocumentValidation on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseDocumentValidation(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentValidation);
    }

    [Obsolete("Use UseExceptions on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseExceptions(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.UnhandledExceptions);
    }

    [Obsolete("Use UseTimeout on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseTimeout(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.Timeout);
    }

    [Obsolete("Use UseInstrumentation on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseInstrumentation(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.Instrumentation);
    }

    [Obsolete("Use UseOperationPlanCache on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseOperationPlanCache(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationPlanCache);
    }

    [Obsolete("Use UseOperationPlan on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseOperationPlan(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationPlan);
    }

    [Obsolete("Use UseConcurrencyGate on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseConcurrencyGate(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.ConcurrencyGate);
    }

    [Obsolete("Use UseOperationExecution on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseOperationExecution(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationExecution);
    }

    [Obsolete("Use UseOperationVariableCoercion on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseOperationVariableCoercion(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationVariableCoercion);
    }

    [Obsolete("Use UseSkipWarmupExecution on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseSkipWarmupExecution(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.SkipWarmupExecution);
    }

    [Obsolete("Use UseReadPersistedOperation on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseReadPersistedOperation(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(
            PersistedOperationMiddleware.ReadPersistedOperation,
            before: before,
            after: after);
    }

    [Obsolete("Use UseAutomaticPersistedOperationNotFound on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseAutomaticPersistedOperationNotFound(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(
            PersistedOperationMiddleware.AutomaticPersistedOperationNotFound,
            before: before,
            after: after);
    }

    [Obsolete("Use UseWritePersistedOperation on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseWritePersistedOperation(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(
            PersistedOperationMiddleware.WritePersistedOperation,
            before: before,
            after: after);
    }

    [Obsolete("Use UsePersistedOperationNotFound on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UsePersistedOperationNotFound(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(
            PersistedOperationMiddleware.PersistedOperationNotFound,
            before: before,
            after: after);
    }

    [Obsolete("Use UseOnlyPersistedOperationAllowed on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseOnlyPersistedOperationAllowed(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(
            PersistedOperationMiddleware.OnlyPersistedOperationsAllowed,
            before: before,
            after: after);
    }

    [Obsolete("Use UseDefaultPipeline on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseDefaultPipeline(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ClearPipeline(builder);

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationPlanCache()
            .UseOperationPlan()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseConcurrencyGate()
            .UseOperationExecution();
    }

    [Obsolete("Use UsePersistedOperationPipeline on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UsePersistedOperationPipeline(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ClearPipeline(builder);

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedOperation()
            .UsePersistedOperationNotFound()
            .UseOnlyPersistedOperationAllowed()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationPlanCache()
            .UseOperationPlan()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseConcurrencyGate()
            .UseOperationExecution();
    }

    [Obsolete("Use UseAutomaticPersistedOperationPipeline on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseAutomaticPersistedOperationPipeline(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ClearPipeline(builder);

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedOperation()
            .UseAutomaticPersistedOperationNotFound()
            .UseWritePersistedOperation()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationPlanCache()
            .UseOperationPlan()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseConcurrencyGate()
            .UseOperationExecution();
    }
}
