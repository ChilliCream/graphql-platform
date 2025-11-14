using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.ModelContextProtocol.Diagnostics;
using HotChocolate.ModelContextProtocol.Handlers;
using HotChocolate.ModelContextProtocol.Proxies;
using HotChocolate.ModelContextProtocol.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class FusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddMcp(
        this IFusionGatewayBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddHttpContextAccessor()
            .AddKeyedSingleton(
                builder.Name,
                static (sp, name) => new McpRequestExecutorProxy(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    (string)name))
            .AddKeyedSingleton(
                builder.Name,
                static (sp, name) => new StreamableHttpHandlerProxy(
                    sp.GetRequiredKeyedService<McpRequestExecutorProxy>(name)))
            .AddKeyedSingleton(
                builder.Name,
                static (sp, name) => new SseHandlerProxy(
                    sp.GetRequiredKeyedService<McpRequestExecutorProxy>(name)));

        builder.ConfigureSchemaServices(
            (_, services) =>
            {
                services
                    .TryAddSingleton(
                        static sp => new OperationToolFactory(
                            sp.GetRequiredService<ISchemaDefinition>()));

                services.TryAddSingleton<IMcpDiagnosticEvents>(sp =>
                {
                    var listeners = sp.GetServices<IMcpDiagnosticEventListener>().ToArray();
                    return listeners.Length switch
                    {
                        0 => new NoopMcpDiagnosticEvents(),
                        1 => listeners[0],
                        _ => new AggregateMcpDiagnosticEvents(listeners)
                    };
                });

                services
                    .TryAddSingleton(
                        static sp => new ToolStorageObserver(
                            sp.GetRequiredService<ISchemaDefinition>(),
                            sp.GetRequiredService<ToolRegistry>(),
                            sp.GetRequiredService<OperationToolFactory>(),
                            sp.GetRequiredService<StreamableHttpHandler>(),
                            sp.GetRequiredService<IOperationToolStorage>(),
                            sp.GetRequiredService<IMcpDiagnosticEvents>()));

                services
                    .AddSingleton(
                        static sp => sp
                            .GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider
                            .GetRequiredService<IHostApplicationLifetime>())
                    .AddSingleton(
                        static sp => sp
                            .GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider
                            .GetRequiredService<ILoggerFactory>())
                    .AddSingleton<ToolRegistry>();

                var mcpServerBuilder =
                    services
                        .AddMcpServer(o =>
                        {
                            configureServerOptions?.Invoke(o);
                            o.Capabilities?.Tools?.ListChanged = true;
                        })
                        .WithHttpTransport()
                        .WithListToolsHandler(
                            (context, _) => ValueTask.FromResult(ListToolsHandler.Handle(context)))
                        .WithCallToolHandler(
                            async (context, cancellationToken)
                                => await CallToolHandler
                                    .HandleAsync(context, cancellationToken)
                                    .ConfigureAwait(false));

                configureServer?.Invoke(mcpServerBuilder);
            });

        builder.AddWarmupTask(async (executor, cancellationToken) =>
        {
            var schema = executor.Schema;
            var storageObserver = schema.Services.GetRequiredService<ToolStorageObserver>();
            await storageObserver.StartAsync(cancellationToken);
        });

        return builder;
    }

    public static IFusionGatewayBuilder AddMcpToolStorage(
        this IFusionGatewayBuilder builder,
        IOperationToolStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.ConfigureSchemaServices((_, s) => s.AddSingleton(storage));

        return builder;
    }
}
