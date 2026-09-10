using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Uses MD5 to hash operation documents.
    /// </summary>
    public static IFusionRouterBuilder AddMD5DocumentHashProvider(
        this IFusionRouterBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        CoreFusionGatewayBuilderExtensions.AddMD5DocumentHashProvider(builder, format);
        return builder;
    }

    /// <summary>
    /// Uses SHA-1 to hash operation documents.
    /// </summary>
    public static IFusionRouterBuilder AddSha1DocumentHashProvider(
        this IFusionRouterBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        CoreFusionGatewayBuilderExtensions.AddSha1DocumentHashProvider(builder, format);
        return builder;
    }

    /// <summary>
    /// Uses SHA-256 to hash operation documents.
    /// </summary>
    public static IFusionRouterBuilder AddSha256DocumentHashProvider(
        this IFusionRouterBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        CoreFusionGatewayBuilderExtensions.AddSha256DocumentHashProvider(builder, format);
        return builder;
    }
}
