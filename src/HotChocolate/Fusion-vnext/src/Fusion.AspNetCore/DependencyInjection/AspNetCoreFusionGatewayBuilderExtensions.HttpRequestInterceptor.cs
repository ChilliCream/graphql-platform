using HotChocolate.AspNetCore;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
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
