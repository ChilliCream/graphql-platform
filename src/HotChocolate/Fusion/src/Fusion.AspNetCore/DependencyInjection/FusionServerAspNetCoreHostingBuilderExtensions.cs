using HotChocolate.Fusion.AspNetCore;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides <see cref="IHostApplicationBuilder"/> extension methods to configure a Fusion GraphQL gateway server.
/// </summary>
public static class FusionServerAspNetCoreHostingBuilderExtensions
{
    /// <summary>
    /// Adds the GraphQL server to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The name of the GraphQL schema.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <param name="disableDefaultSecurity">
    /// Defines if the default security policy should be disabled.
    /// </param>
    /// <returns>
    /// The <see cref="IFusionGatewayBuilder"/> for configuration chaining.
    /// </returns>
    public static IFusionGatewayBuilder AddGraphQLGateway(
        this IHostApplicationBuilder builder,
        string? name = null,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize,
        bool disableDefaultSecurity = false)
        => builder.Services.AddGraphQLGatewayServer(name, maxAllowedRequestSize, disableDefaultSecurity);
}
