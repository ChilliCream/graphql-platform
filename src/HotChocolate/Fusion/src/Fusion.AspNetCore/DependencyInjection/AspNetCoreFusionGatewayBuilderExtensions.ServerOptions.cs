using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the <see cref="GraphQLServerOptions"/>
    /// (GET requests, multipart, schema requests, batching, tool options, etc.).
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="GraphQLServerOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder ModifyServerOptions(
        this IFusionGatewayBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(builder.Name, configure);
        return builder;
    }

    /// <summary>
    /// Registers a callback to modify the <see cref="GraphQLServerOptions"/>
    /// (GET requests, multipart, schema requests, batching, tool options, etc.),
    /// with access to the application <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="GraphQLServerOptions"/>,
    /// with access to the application <see cref="IServiceProvider"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder ModifyServerOptions(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, GraphQLServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services
            .AddOptions<GraphQLServerOptions>(builder.Name)
            .Configure<IServiceProvider>((options, sp) => configure(sp, options));
        return builder;
    }
}
