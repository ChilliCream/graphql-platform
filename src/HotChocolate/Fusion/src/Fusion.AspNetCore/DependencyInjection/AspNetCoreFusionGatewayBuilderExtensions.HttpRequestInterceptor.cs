using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Transport.Formatters;
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

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of
    /// the schema services. If your <typeparamref name="T"/> needs to access application services
    /// you need to make the services available in the schema services via
    /// <see cref="CoreFusionGatewayBuilderExtensions.AddApplicationService"/>.
    /// </remarks>
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, ISocketSessionInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<ISocketSessionInterceptor>();
                s.AddSingleton<ISocketSessionInterceptor, T>();
            });
    }

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the interceptor instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
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
    public static IFusionGatewayBuilder AddSocketSessionInterceptor<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<ISocketSessionInterceptor>();
                s.AddSingleton<ISocketSessionInterceptor, T>(factory);
            });
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without extra white spaces.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one
    /// via the <c>Accept</c> header. Defaults to <see cref="IncrementalDeliveryFormat.Version_0_2"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        bool indented = false,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                        new HttpResponseFormatterOptions
                        {
                            Json = new JsonResultFormatterOptions
                            {
                                Indented = indented
                            }
                        },
                        sp.GetRequiredService<ITimeProvider>(),
                        incrementalDeliveryFormat));
            });
    }

    /// <summary>
    /// Adds the <see cref="DefaultHttpResponseFormatter"/> with specific formatter options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The HTTP response formatter options.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the client does not specify one
    /// via the <c>Accept</c> header. Defaults to <see cref="IncrementalDeliveryFormat.Version_0_2"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpResponseFormatter(
        this IFusionGatewayBuilder builder,
        HttpResponseFormatterOptions options,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(
                    sp => DefaultHttpResponseFormatter.Create(
                        options,
                        sp.GetRequiredService<ITimeProvider>(),
                        incrementalDeliveryFormat));
            });
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpResponseFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter, T>();
            });
    }

    /// <summary>
    /// Adds a custom HTTP response formatter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The service factory.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IHttpResponseFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpResponseFormatter<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IHttpResponseFormatter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            (_, s) =>
            {
                s.RemoveAll<IHttpResponseFormatter>();
                s.AddSingleton<IHttpResponseFormatter>(factory);
            });
    }
}
