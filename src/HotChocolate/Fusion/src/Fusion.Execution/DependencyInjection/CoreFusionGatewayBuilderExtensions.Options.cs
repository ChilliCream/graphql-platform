using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Planning;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Fusion gateway options on <see cref="IFusionGatewayBuilder"/>.
/// </summary>
public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the core <see cref="FusionOptions"/>
    /// (cache sizes, eviction timeout, error handling mode, etc.).
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="configure">A delegate that configures the options.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    public static IFusionGatewayBuilder ModifyOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.OptionsModifiers.Add(configure));
    }

    /// <summary>
    /// Registers a callback to modify the <see cref="FusionRequestOptions"/>
    /// (persisted operations, exception details, etc.).
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="configure">A delegate that configures the request options.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    public static IFusionGatewayBuilder ModifyRequestOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionRequestOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.RequestOptionsModifiers.Add(configure));
    }

    /// <summary>
    /// Registers a callback to modify the <see cref="OperationPlannerOptions"/>
    /// (planning guardrails such as max planning time, max expanded nodes, etc.).
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="configure">A delegate that configures the planner options.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    public static IFusionGatewayBuilder ModifyPlannerOptions(
        this IFusionGatewayBuilder builder,
        Action<OperationPlannerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.PlannerOptionsModifiers.Add(configure));
    }
}
