using HotChocolate.Caching;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

// The legacy extension surface holds the shared operations until the compatibility surface
// is removed. Calling it here forwards through the same configuration pipeline.
#pragma warning disable CS0618

/// <summary>
/// Provides extension methods for <see cref="IFusionRouterBuilder"/>
/// to add cache control support.
/// </summary>
public static class FusionCachingRouterBuilderExtensions
{
    /// <summary>
    /// Registers the query cache middleware in the router pipeline.
    /// </summary>
    public static IFusionRouterBuilder UseQueryCache(
        this IFusionRouterBuilder builder,
        string? after = null)
    {
        FusionCachingGatewayBuilderExtensions.UseQueryCache(builder, after);
        return builder;
    }

    /// <summary>
    /// Adds cache control support to the router, including the planner
    /// interceptor that computes cache constraints.
    /// </summary>
    public static IFusionRouterBuilder AddCacheControl(
        this IFusionRouterBuilder builder)
    {
        FusionCachingGatewayBuilderExtensions.AddCacheControl(builder);
        return builder;
    }

    /// <summary>
    /// Modifies the <see cref="CacheControlOptions"/>.
    /// </summary>
    public static IFusionRouterBuilder ModifyCacheControlOptions(
        this IFusionRouterBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        FusionCachingGatewayBuilderExtensions.ModifyCacheControlOptions(builder, modifyOptions);
        return builder;
    }
}
