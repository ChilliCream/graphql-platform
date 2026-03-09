using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Pipeline;
using static HotChocolate.Fusion.Configuration.FusionSetupUtilities;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder UseDocumentCache(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentCache);
    }

    public static IFusionGatewayBuilder UseDocumentParser(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentParser);
    }

    public static IFusionGatewayBuilder UseDocumentValidation(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentValidation);
    }

    public static IFusionGatewayBuilder UseExceptions(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.UnhandledExceptions);
    }

    public static IFusionGatewayBuilder UseTimeout(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.Timeout);
    }

    public static IFusionGatewayBuilder UseInstrumentation(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.Instrumentation);
    }

    public static IFusionGatewayBuilder UseOperationPlanCache(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationPlanCache);
    }

    public static IFusionGatewayBuilder UseOperationPlan(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationPlan);
    }

    public static IFusionGatewayBuilder UseOperationExecution(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationExecution);
    }

    public static IFusionGatewayBuilder UseOperationVariableCoercion(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(FusionMiddleware.OperationVariableCoercion);
    }

    public static IFusionGatewayBuilder UseSkipWarmupExecution(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.SkipWarmupExecution);
    }

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
            .UseOperationExecution();
    }

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
            .UseOperationExecution();
    }

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
            .UseOperationExecution();
    }
}
