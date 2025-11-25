using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

public static class FusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddMcp(
        this IFusionGatewayBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMcpServices(builder.Name);

        builder.ConfigureSchemaServices(
            (_, services) => services.AddMcpSchemaServices(configureServerOptions, configureServer));

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
