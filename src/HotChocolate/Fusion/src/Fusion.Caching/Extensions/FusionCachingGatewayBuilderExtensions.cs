using HotChocolate.Caching;
using HotChocolate.Execution;
using HotChocolate.Fusion.Caching;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IFusionGatewayBuilder"/>
/// to add cache control support.
/// </summary>
// TODO [17]: Remove the legacy extension surface after the 16.x compatibility window.
[Obsolete("Use FusionCachingRouterBuilderExtensions instead.")]
public static class FusionCachingGatewayBuilderExtensions
{
    /// <summary>
    /// Registers the query cache middleware in the Fusion gateway pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="after">
    /// The middleware key after which to insert the query cache middleware.
    /// Defaults to the timeout middleware.
    /// </param>
    /// <returns>
    /// The <see cref="IFusionGatewayBuilder"/> for chaining.
    /// </returns>
    [Obsolete("Use UseQueryCache on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder UseQueryCache(
        this IFusionGatewayBuilder builder,
        string? after = null)
        => builder.UseRequest(
            QueryCacheMiddleware.Create(),
            after: after ?? WellKnownRequestMiddleware.TimeoutMiddleware);

    /// <summary>
    /// Adds cache control support to the Fusion gateway, including
    /// the planner interceptor that computes cache constraints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <returns>
    /// The <see cref="IFusionGatewayBuilder"/> for chaining.
    /// </returns>
    [Obsolete("Use AddCacheControl on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddCacheControl(
        this IFusionGatewayBuilder builder)
    {
        builder.ConfigureSchemaServices(
            (_, services) =>
            {
                services.AddOptions();
                services.TryAddSingleton<ICacheControlOptionsAccessor, CacheControlOptionsAccessor>();
            });

        builder.AddOperationPlannerInterceptor(
            _ => new CacheControlPlannerInterceptor());

        return builder;
    }

    /// <summary>
    /// Modifies the <see cref="CacheControlOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="modifyOptions">
    /// A delegate to configure the <see cref="CacheControlOptions"/>.
    /// </param>
    /// <returns>
    /// The <see cref="IFusionGatewayBuilder"/> for chaining.
    /// </returns>
    [Obsolete("Use ModifyCacheControlOptions on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ModifyCacheControlOptions(
        this IFusionGatewayBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        ArgumentNullException.ThrowIfNull(modifyOptions);

        builder.ConfigureSchemaServices(
            (_, services) => services.Configure(modifyOptions));

        return builder;
    }
}
