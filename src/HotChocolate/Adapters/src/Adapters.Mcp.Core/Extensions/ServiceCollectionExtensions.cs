using System.Collections.Concurrent;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Handlers;
using HotChocolate.Adapters.Mcp.Proxies;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class ServiceCollectionExtensions
{
    public static void AddMcpServices(this IServiceCollection services, string schemaName)
    {
        services
            .AddHttpContextAccessor()
            .AddKeyedSingleton(
                schemaName,
                static (sp, name) => new McpRequestExecutorProxy(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    (string)name!))
            .AddKeyedSingleton(
                schemaName,
                static (sp, name) => new StreamableHttpHandlerProxy(
                    sp.GetRequiredKeyedService<McpRequestExecutorProxy>(name)))
            .AddKeyedSingleton(
                schemaName,
                static (sp, name) => new SseHandlerProxy(
                    sp.GetRequiredKeyedService<McpRequestExecutorProxy>(name)));
    }

    public static void AddMcpSchemaServices(
        this IServiceCollection services,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        services
            .AddLogging()
            .TryAddSingleton(
                static sp => new OperationToolFactory(sp.GetRequiredService<ISchemaDefinition>()));

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
                static sp => new McpStorageObserver(
                    sp.GetRequiredService<ISchemaDefinition>(),
                    sp.GetRequiredService<McpFeatureRegistry>(),
                    sp.GetRequiredService<OperationToolFactory>(),
                    sp.GetRequiredService<ConcurrentDictionary<string, McpServer>>(),
                    sp.GetRequiredService<IMcpStorage>(),
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
            .AddSingleton<McpFeatureRegistry>();

        var mcpServers = new ConcurrentDictionary<string, McpServer>();
        services.AddSingleton(mcpServers);

        var mcpServerBuilder =
            services
                .AddMcpServer(options =>
                {
                    configureServerOptions?.Invoke(options);
                    options.Capabilities?.Prompts?.ListChanged = true;
                    options.Capabilities?.Tools?.ListChanged = true;
                })
                .WithHttpTransport(options =>
                {
                    options.RunSessionHandler = async (_, mcpServer, token) =>
                    {
                        if (mcpServer.SessionId == null)
                        {
                            // There is no sessionId if serverOptions.Stateless is true.
                            await mcpServer.RunAsync(token);
                            return;
                        }

                        try
                        {
                            mcpServers[mcpServer.SessionId] = mcpServer;
                            await mcpServer.RunAsync(token);
                        }
                        finally
                        {
                            // This code runs when the session ends.
                            mcpServers.TryRemove(mcpServer.SessionId, out var _);
                        }
                    };
                })
                .WithListPromptsHandler(
                    (context, _) => ValueTask.FromResult(ListPromptsHandler.Handle(context)))
                .WithGetPromptHandler(
                    (context, _) => ValueTask.FromResult(GetPromptHandler.Handle(context)))
                .WithReadResourceHandler(
                    (context, _) => ValueTask.FromResult(ReadResourceHandler.Handle(context)))
                .WithListToolsHandler(
                    (context, _) => ValueTask.FromResult(ListToolsHandler.Handle(context)))
                .WithCallToolHandler(CallToolHandler.HandleAsync);

        configureServer?.Invoke(mcpServerBuilder);
    }
}
