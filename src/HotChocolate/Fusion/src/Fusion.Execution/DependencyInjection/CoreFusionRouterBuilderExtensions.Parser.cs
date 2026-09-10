using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the router parser options.
    /// </summary>
    public static IFusionRouterBuilder ModifyParserOptions(
        this IFusionRouterBuilder builder,
        Action<FusionParserOptions> configure)
    {
        CoreFusionGatewayBuilderExtensions.ModifyParserOptions(builder, configure);
        return builder;
    }
}
