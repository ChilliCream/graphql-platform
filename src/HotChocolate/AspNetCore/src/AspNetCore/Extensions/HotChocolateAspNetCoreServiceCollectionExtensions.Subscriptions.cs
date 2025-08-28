using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddSocketSessionInterceptor<T>(
        this IRequestExecutorBuilder builder)
        where T : class, ISocketSessionInterceptor =>
        builder.ConfigureSchemaServices(s => s
            .RemoveAll<ISocketSessionInterceptor>()
            .AddSingleton<ISocketSessionInterceptor, T>(
                sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp.GetCombinedServices())));

    /// <summary>
    /// Adds an interceptor for GraphQL socket sessions to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the interceptor instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="ISocketSessionInterceptor"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddSocketSessionInterceptor<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, ISocketSessionInterceptor =>
        builder.ConfigureSchemaServices(s => s
            .RemoveAll<ISocketSessionInterceptor>()
            .AddSingleton<ISocketSessionInterceptor, T>(sp => factory(sp.GetCombinedServices())));

    private static IRequestExecutorBuilder AddSubscriptionServices(
        this IRequestExecutorBuilder builder)
        => builder
            .ConfigureSchemaServices(s =>
            {
                s.TryAddSingleton<ISocketSessionInterceptor, DefaultSocketSessionInterceptor>();
                s.TryAddSingleton<IWebSocketPayloadFormatter>(_ => new DefaultWebSocketPayloadFormatter());
            })
            .AddApolloProtocol()
            .AddGraphQLOverWebSocketProtocol();

    private static IRequestExecutorBuilder AddApolloProtocol(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchemaServices(
            s => s.AddSingleton<IProtocolHandler>(
                sp => new ApolloSubscriptionProtocolHandler(
                    sp.GetRequiredService<ISocketSessionInterceptor>(),
                    sp.GetRequiredService<IWebSocketPayloadFormatter>())));

    private static IRequestExecutorBuilder AddGraphQLOverWebSocketProtocol(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchemaServices(
            s => s.AddSingleton<IProtocolHandler>(
                sp => new GraphQLOverWebSocketProtocolHandler(
                    sp.GetRequiredService<ISocketSessionInterceptor>(),
                    sp.GetRequiredService<IWebSocketPayloadFormatter>())));

    /// <summary>
    /// Adds a custom WebSocket payload formatter to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IWebSocketPayloadFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddWebSocketPayloadFormatter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IWebSocketPayloadFormatter
    {
        builder.ConfigureSchemaServices(services =>
        {
            services.RemoveAll<IWebSocketPayloadFormatter>();
            services.AddSingleton<IWebSocketPayloadFormatter, T>();
        });

        return builder;
    }

    /// <summary>
    /// Adds a custom WebSocket payload formatter to the DI.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The service factory.
    /// </param>
    /// <typeparam name="T">
    /// The type of the custom <see cref="IWebSocketPayloadFormatter"/>.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddWebSocketPayloadFormatter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IWebSocketPayloadFormatter
    {
        builder.ConfigureSchemaServices(services =>
        {
            services.RemoveAll<IWebSocketPayloadFormatter>();
            services.AddSingleton<IWebSocketPayloadFormatter>(factory);
        });

        return builder;
    }
}
