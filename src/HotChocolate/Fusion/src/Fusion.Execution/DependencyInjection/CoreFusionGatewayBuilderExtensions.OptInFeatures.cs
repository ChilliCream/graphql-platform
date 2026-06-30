using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Enables opt-in feature support on the gateway. When enabled, the introspection schema
    /// exposes the <c>includeOptIn</c> argument and opt-in members are hidden from introspection
    /// unless the client opts into their feature.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    public static IFusionGatewayBuilder EnableOptInFeatures(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ModifyOptions(o => o.EnableOptInFeatures = true);
    }
}
