using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IHttpRequestInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpRequestInterceptor<T>(
        this IFusionGatewayBuilder builder)
        where T : IHttpRequestInterceptor, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton<IHttpRequestInterceptor>(new T());
            });
    }

    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the interceptor instance.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="CoreFusionGatewayBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IFusionGatewayBuilder AddHttpRequestInterceptor(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IHttpRequestInterceptor> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton(factory);
            });
    }
}
