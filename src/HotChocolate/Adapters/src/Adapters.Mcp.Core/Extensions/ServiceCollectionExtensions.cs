using System.Collections.Concurrent;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Handlers;
using HotChocolate.Adapters.Mcp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal static class ServiceCollectionExtensions
{
    public static void AddMcpServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddOptions();
        services.TryAddSingleton<McpManager>();
        services.TryAddSingleton<IMcpProvider>(static sp => sp.GetRequiredService<McpManager>());
    }

    public static void ConfigureMcpSetup(
        this IServiceCollection services,
        string schemaName,
        Action<McpServerOptions>? configureServerOptions,
        Action<IMcpServerBuilder>? configureServer)
    {
        // Always register a named configuration entry, even if both callbacks are null,
        // so that McpManager.Names can discover the schema via IConfigureNamedOptions<McpSetup>.
        services.Configure<McpSetup>(
            schemaName,
            setup =>
            {
                if (configureServerOptions is not null)
                {
                    setup.ServerOptionsModifiers.Add(configureServerOptions);
                }

                if (configureServer is not null)
                {
                    setup.ServerModifiers.Add(configureServer);
                }
            });
    }

    public static void AddMcpSchemaServices(
        this IServiceCollection services,
        IServiceProvider applicationServices,
        string schemaName)
    {
        var setup = applicationServices
            .GetRequiredService<McpManager>()
            .GetSetup(schemaName);

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
                    foreach (var modifier in setup.ServerOptionsModifiers)
                    {
                        modifier(options);
                    }

                    options.Capabilities?.Prompts?.ListChanged = true;
                    options.Capabilities?.Tools?.ListChanged = true;
                })
                .WithHttpTransport(options =>
                {
#pragma warning disable MCPEXP002 // https://github.com/modelcontextprotocol/csharp-sdk/issues/1416
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
#pragma warning restore MCPEXP002
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

        foreach (var modifier in setup.ServerModifiers)
        {
            modifier(mcpServerBuilder);
        }
    }
}
