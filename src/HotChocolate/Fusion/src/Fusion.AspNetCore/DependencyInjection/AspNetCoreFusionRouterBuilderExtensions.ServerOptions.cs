using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the <see cref="GraphQLServerOptions"/>
    /// (GET requests, multipart, schema requests, batching, tool options, etc.).
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionRouterBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="GraphQLServerOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionRouterBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionRouterBuilder ModifyServerOptions(
        this IFusionRouterBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        AspNetCoreFusionBuilderConfiguration.ModifyServerOptions(builder, configure);
        return builder;
    }
}
