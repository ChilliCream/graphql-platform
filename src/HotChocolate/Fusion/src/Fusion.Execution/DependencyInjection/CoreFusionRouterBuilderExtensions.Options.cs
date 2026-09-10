using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Planning;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the core router options.
    /// </summary>
    public static IFusionRouterBuilder ModifyOptions(
        this IFusionRouterBuilder builder,
        Action<FusionOptions> configure)
    {
        CoreFusionGatewayBuilderExtensions.ModifyOptions(builder, configure);
        return builder;
    }

    /// <summary>
    /// Registers a callback to modify the router request options.
    /// </summary>
    public static IFusionRouterBuilder ModifyRequestOptions(
        this IFusionRouterBuilder builder,
        Action<FusionRequestOptions> configure)
    {
        CoreFusionGatewayBuilderExtensions.ModifyRequestOptions(builder, configure);
        return builder;
    }

    /// <summary>
    /// Registers a callback to modify the operation planner options.
    /// </summary>
    public static IFusionRouterBuilder ModifyPlannerOptions(
        this IFusionRouterBuilder builder,
        Action<OperationPlannerOptions> configure)
    {
        CoreFusionGatewayBuilderExtensions.ModifyPlannerOptions(builder, configure);
        return builder;
    }
}
