using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    [Obsolete("Use AddMD5DocumentHashProvider on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddMD5DocumentHashProvider(
        this IFusionGatewayBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices((_, services) =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new MD5DocumentHashProvider(format));
        });
    }

    [Obsolete("Use AddSha1DocumentHashProvider on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSha1DocumentHashProvider(
        this IFusionGatewayBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices((_, services) =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new Sha1DocumentHashProvider(format));
        });
    }

    [Obsolete("Use AddSha256DocumentHashProvider on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSha256DocumentHashProvider(
        this IFusionGatewayBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices((_, services) =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new Sha256DocumentHashProvider(format));
        });
    }
}
