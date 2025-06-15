using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Pipeline;

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

        return builder.UseRequest(TimeoutMiddleware.Create());
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

        return builder.UseRequest(OperationPlanCacheMiddleware.Create());
    }

    public static IFusionGatewayBuilder UseOperationPlan(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationPlanMiddleware.Create());
    }

    public static IFusionGatewayBuilder UseOperationExecution(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationExecutionMiddleware.Create());
    }

    public static IFusionGatewayBuilder UseOperationVariableCoercion(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationVariableCoercionMiddleware.Create());
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

        if(after is not null && before is not null)
        {
            throw new ArgumentException(
                "You cannot specify both 'after' and 'before' parameters.");
        }

        if (after is not null)
        {
            return builder.AppendUseRequest(
                after,
                PersistedOperationMiddleware.ReadPersistedOperation);
        }

        if (before is not null)
        {
            return builder.InsertUseRequest(
                before,
                PersistedOperationMiddleware.ReadPersistedOperation);
        }

        return builder.UseRequest(PersistedOperationMiddleware.ReadPersistedOperation);
    }

    public static IFusionGatewayBuilder UseAutomaticPersistedOperationNotFound(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if(after is not null && before is not null)
        {
            throw new ArgumentException(
                "You cannot specify both 'after' and 'before' parameters.");
        }

        if (after is not null)
        {
            return builder.AppendUseRequest(
                after,
                PersistedOperationMiddleware.AutomaticPersistedOperationNotFound);
        }

        if (before is not null)
        {
            return builder.InsertUseRequest(
                before,
                PersistedOperationMiddleware.AutomaticPersistedOperationNotFound);
        }

        return builder.UseRequest(PersistedOperationMiddleware.AutomaticPersistedOperationNotFound);
    }

    public static IFusionGatewayBuilder UseWritePersistedOperation(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if(after is not null && before is not null)
        {
            throw new ArgumentException(
                "You cannot specify both 'after' and 'before' parameters.");
        }

        if (after is not null)
        {
            return builder.AppendUseRequest(
                after,
                PersistedOperationMiddleware.WritePersistedOperation);
        }

        if (before is not null)
        {
            return builder.InsertUseRequest(
                before,
                PersistedOperationMiddleware.WritePersistedOperation);
        }

        return builder.UseRequest(PersistedOperationMiddleware.WritePersistedOperation);
    }

    public static IFusionGatewayBuilder UsePersistedOperationNotFound(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if(after is not null && before is not null)
        {
            throw new ArgumentException(
                "You cannot specify both 'after' and 'before' parameters.");
        }

        if (after is not null)
        {
            return builder.AppendUseRequest(
                after,
                PersistedOperationMiddleware.PersistedOperationNotFound);
        }

        if (before is not null)
        {
            return builder.InsertUseRequest(
                before,
                PersistedOperationMiddleware.PersistedOperationNotFound);
        }

        return builder.UseRequest(PersistedOperationMiddleware.PersistedOperationNotFound);
    }

    public static IFusionGatewayBuilder UseOnlyPersistedOperationAllowed(
        this IFusionGatewayBuilder builder,
        string? after = null,
        string? before = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if(after is not null && before is not null)
        {
            throw new ArgumentException(
                "You cannot specify both 'after' and 'before' parameters.");
        }

        if (after is not null)
        {
            return builder.AppendUseRequest(
                after,
                PersistedOperationMiddleware.OnlyPersistedOperationsAllowed);
        }

        if (before is not null)
        {
            return builder.InsertUseRequest(
                before,
                PersistedOperationMiddleware.OnlyPersistedOperationsAllowed);
        }

        return builder.UseRequest(PersistedOperationMiddleware.OnlyPersistedOperationsAllowed);
    }

    public static IFusionGatewayBuilder UseDefaultPipeline(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FusionGatewayBuilderUtilities.ClearPipeline(builder);

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationPlanCache()
            .UseOperationPlan()
            .UseOperationExecution()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
    }

    public static IFusionGatewayBuilder UsePersistedOperationPipeline(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FusionGatewayBuilderUtilities.ClearPipeline(builder);

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

        FusionGatewayBuilderUtilities.ClearPipeline(builder);

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
