using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

// TODO [17]: Remove the legacy extension surface after the 16.x compatibility window.
[Obsolete("Use AspNetCoreFusionRouterBuilderExtensions instead.")]
public static partial class AspNetCoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <typeparam name="T">The <see cref="IHttpRequestInterceptor"/> implementation.</typeparam>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpRequestInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpRequestInterceptor<T>(
        this IFusionGatewayBuilder builder)
        where T : IHttpRequestInterceptor, new()
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL over HTTP requests.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="factory">A factory that creates the interceptor instance.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpRequestInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpRequestInterceptor(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IHttpRequestInterceptor> factory)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpRequestInterceptor(builder, factory);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <typeparam name="T">The <see cref="ISocketSessionInterceptor"/> implementation.</typeparam>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddSocketSessionInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="factory">A factory that creates the interceptor instance.</param>
    /// <typeparam name="T">The <see cref="ISocketSessionInterceptor"/> implementation.</typeparam>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddSocketSessionInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor
    {
        AspNetCoreFusionBuilderConfiguration.AddSocketSessionInterceptor(builder, factory);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="indented">
    /// Defines whether the underlying JSON writer should pretty print the JSON output.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one.
    /// </param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        bool indented = false,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, indented, incrementalDeliveryFormat);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="options">The HTTP response formatter options.</param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one.
    /// </param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        HttpResponseFormatterOptions options,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, options, incrementalDeliveryFormat);
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <typeparam name="T">The type of the custom <see cref="IHttpResponseFormatter"/>.</typeparam>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">The gateway builder.</param>
    /// <param name="factory">The service factory.</param>
    /// <typeparam name="T">The type of the custom <see cref="IHttpResponseFormatter"/>.</typeparam>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for chaining.</returns>
    [Obsolete("Use AddHttpResponseFormatter on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddHttpResponseFormatter<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        AspNetCoreFusionBuilderConfiguration.AddHttpResponseFormatter(builder, factory);
        return builder;
    }
}
