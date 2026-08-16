using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.Execution;
using HotChocolate.Fusion.AspNetCore;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class FusionServerServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Fusion GraphQL router with the GraphQL server transport to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the GraphQL schema, <c>null</c> for the default schema.</param>
    /// <param name="maxAllowedRequestSize">The max allowed GraphQL request size.</param>
    /// <param name="disableDefaultSecurity">Defines if the default security policy should be disabled.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for configuration chaining.</returns>
    public static IFusionGatewayBuilder AddGraphQLRouter(
        this IServiceCollection services,
        string? name = null,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize,
        bool disableDefaultSecurity = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfNegative(maxAllowedRequestSize);

        var builder = services
            .AddGraphQLRouterCore(name)
            .AddHttpTransport(maxAllowedRequestSize)
            .AddServerDiagnostics()
            .AddExecutionConcurrencyGate()
            .AddStartupInitialization()
            .AddDefaultHttpRequestInterceptor()
            .AddSubscriptionServices();

        if (!disableDefaultSecurity)
        {
            builder.DisableIntrospection(
                (sp, _) =>
                {
                    var environment = sp.GetService<IHostEnvironment>();
                    return environment?.IsDevelopment() != true;
                });
            builder.AddMaxAllowedFieldCycleDepthRule();
        }

        return builder;
    }

    /// <summary>
    /// Adds a Fusion GraphQL router with the GraphQL server transport to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the GraphQL schema, <c>null</c> for the default schema.</param>
    /// <param name="maxAllowedRequestSize">The max allowed GraphQL request size.</param>
    /// <param name="disableDefaultSecurity">Defines if the default security policy should be disabled.</param>
    /// <returns>The <see cref="IFusionGatewayBuilder"/> for configuration chaining.</returns>
    [Obsolete("Use AddGraphQLRouter() instead.")]
    public static IFusionGatewayBuilder AddGraphQLGatewayServer(
        this IServiceCollection services,
        string? name = null,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize,
        bool disableDefaultSecurity = false)
        => services.AddGraphQLRouter(name, maxAllowedRequestSize, disableDefaultSecurity);

    private static IFusionGatewayBuilder AddHttpTransport(
        this IFusionGatewayBuilder builder,
        int maxAllowedRequestSize)
        => builder.ConfigureSchemaServices((_, sc) =>
        {
            sc.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();

            sc.TryAddSingleton<IHttpResponseFormatter>(
                sp => DefaultHttpResponseFormatter.Create(
                    new HttpResponseFormatterOptions { HttpTransportVersion = HttpTransportVersion.Latest },
                    sp.GetRequiredService<ITimeProvider>(),
                    IncrementalDeliveryFormat.Version_0_2));

            sc.TryAddSingleton<IHttpRequestParser>(
                sp => new DefaultHttpRequestParser(
                    sp.GetRequiredService<IDocumentCache>(),
                    sp.GetRequiredService<IDocumentHashProvider>(),
                    maxAllowedRequestSize,
                    sp.GetRequiredService<ParserOptions>()));
        });

    private static IFusionGatewayBuilder AddServerDiagnostics(
        this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            (_, sc) => sc.TryAddSingleton<IServerDiagnosticEvents>(sp =>
            {
                var listeners = sp.GetServices<IServerDiagnosticEventListener>().ToArray();
                return listeners.Length switch
                {
                    0 => new NoopServerDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateServerDiagnosticEventListener(listeners)
                };
            }));

    private static IFusionGatewayBuilder AddExecutionConcurrencyGate(
        this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            (applicationServices, sc) => sc.TryAddSingleton(schemaServices =>
            {
                var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                var serverOptions = applicationServices
                    .GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>()
                    .Get(schemaName);
                return new ExecutionConcurrencyGate(serverOptions.MaxConcurrentExecutions);
            }));

    private static IFusionGatewayBuilder AddStartupInitialization(
        this IFusionGatewayBuilder builder)
    {
        builder.Services.AddHostedService<FusionRequestExecutorWarmupService>();

        return builder;
    }

    private static IFusionGatewayBuilder AddDefaultHttpRequestInterceptor(
        this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            (_, s) => s.TryAddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>());

    private static IFusionGatewayBuilder AddSubscriptionServices(
        this IFusionGatewayBuilder builder)
        => builder
            .ConfigureSchemaServices((_, s) =>
            {
                s.TryAddSingleton<ISocketSessionInterceptor, DefaultSocketSessionInterceptor>();
                s.TryAddSingleton<IWebSocketPayloadFormatter>(_ => new DefaultWebSocketPayloadFormatter());
            })
            .AddApolloProtocol()
            .AddGraphQLOverWebSocketProtocol();

    private static IFusionGatewayBuilder AddApolloProtocol(
        this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            (_, s) => s.AddSingleton<IProtocolHandler>(
                sp => new ApolloSubscriptionProtocolHandler(
                    sp.GetRequiredService<ISocketSessionInterceptor>(),
                    sp.GetRequiredService<IWebSocketPayloadFormatter>())));

    private static IFusionGatewayBuilder AddGraphQLOverWebSocketProtocol(
        this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            (_, s) => s.AddSingleton<IProtocolHandler>(
                sp => new GraphQLOverWebSocketProtocolHandler(
                    sp.GetRequiredService<ISocketSessionInterceptor>(),
                    sp.GetRequiredService<IWebSocketPayloadFormatter>(),
                    sp.GetRequiredService<IDocumentCache>(),
                    sp.GetRequiredService<IDocumentHashProvider>(),
                    sp.GetRequiredService<ParserOptions>())));
}
