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

namespace Microsoft.Extensions.DependencyInjection;

public static class FusionServerServiceCollectionExtensions
{
    public static IFusionGatewayBuilder AddGraphQLGatewayServer(
        this IServiceCollection services,
        string? name = null,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfNegative(maxAllowedRequestSize);

        return services
            .AddGraphQLGateway(name)
            .AddGraphQLGatewayServerCore()
            .AddDefaultHttpRequestInterceptor()
            .AddSubscriptionServices();
    }

    private static IFusionGatewayBuilder AddGraphQLGatewayServerCore(
        this IFusionGatewayBuilder builder,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize)
    {
        builder.ConfigureSchemaServices((_, sc) =>
        {
            sc.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();

            sc.TryAddSingleton<IHttpResponseFormatter>(
                sp => DefaultHttpResponseFormatter.Create(
                    new HttpResponseFormatterOptions { HttpTransportVersion = HttpTransportVersion.Latest },
                    sp.GetRequiredService<ITimeProvider>()));

            sc.TryAddSingleton<IHttpRequestParser>(
                sp => new DefaultHttpRequestParser(
                    sp.GetRequiredService<IDocumentCache>(),
                    sp.GetRequiredService<IDocumentHashProvider>(),
                    maxAllowedRequestSize,
                    sp.GetRequiredService<ParserOptions>()));

            sc.TryAddSingleton<IServerDiagnosticEvents>(sp =>
            {
                var listeners = sp.GetServices<IServerDiagnosticEventListener>().ToArray();
                return listeners.Length switch
                {
                    0 => new NoopServerDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateServerDiagnosticEventListener(listeners)
                };
            });
        });

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
                    sp.GetRequiredService<IWebSocketPayloadFormatter>())));
}
