using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.Execution;
using HotChocolate.Fusion.AspNetCore;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class FusionServerServiceCollectionExtensions
{
    private static IFusionGatewayBuilder AddGraphQLGatewayServerCore(
        this IFusionGatewayBuilder builder,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize)
    {
        builder.ConfigureSchemaServices((_, sc) =>
        {
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

    public static IFusionGatewayBuilder AddGraphQLGatewayServer(
        this IServiceCollection services,
        string? name = null,
        int maxAllowedRequestSize = ServerDefaults.MaxAllowedRequestSize)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfNegative(maxAllowedRequestSize);

        return services
            .AddGraphQLGateway(name)
            .AddGraphQLGatewayServerCore();
    }
}
