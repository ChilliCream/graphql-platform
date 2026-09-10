using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the <see cref="GraphQLServerOptions"/>
    /// (GET requests, multipart, schema requests, batching, tool options, etc.).
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="configure">A delegate that is used to modify the <see cref="GraphQLServerOptions"/>.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use ModifyServerOptions on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ModifyServerOptions(
        this IFusionGatewayBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        AspNetCoreFusionBuilderConfiguration.ModifyServerOptions(builder, configure);
        return builder;
    }
}
