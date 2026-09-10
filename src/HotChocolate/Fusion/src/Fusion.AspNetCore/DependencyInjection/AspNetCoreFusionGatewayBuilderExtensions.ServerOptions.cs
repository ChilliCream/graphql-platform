using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <inheritdoc cref="AspNetCoreFusionRouterBuilderExtensions.ModifyServerOptions"/>
    [Obsolete("Use ModifyServerOptions on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ModifyServerOptions(
        this IFusionGatewayBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        AspNetCoreFusionBuilderConfiguration.ModifyServerOptions(builder, configure);
        return builder;
    }
}
