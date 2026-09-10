using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds middleware to the router request pipeline, optionally before or after a keyed middleware.
    /// </summary>
    public static IFusionRouterBuilder UseRequest(
        this IFusionRouterBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        CoreFusionGatewayBuilderExtensions.UseRequest(builder, middleware, key, before, after, allowMultiple);
        return builder;
    }

    /// <summary>
    /// Adds middleware to the router request pipeline, optionally before or after a keyed middleware.
    /// </summary>
    public static IFusionRouterBuilder UseRequest(
        this IFusionRouterBuilder builder,
        RequestMiddleware middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        CoreFusionGatewayBuilderExtensions.UseRequest(builder, middleware, key, before, after, allowMultiple);
        return builder;
    }

    /// <summary>
    /// Adds configured middleware to the router pipeline, optionally before or after a keyed middleware.
    /// </summary>
    public static IFusionRouterBuilder UseRequest(
        this IFusionRouterBuilder builder,
        RequestMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        CoreFusionGatewayBuilderExtensions.UseRequest(builder, configuration, before, after, allowMultiple);
        return builder;
    }
}
