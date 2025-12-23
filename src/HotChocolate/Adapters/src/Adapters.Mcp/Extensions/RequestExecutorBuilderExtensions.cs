using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(
        this IRequestExecutorBuilder builder,
        Action<McpServerOptions>? configureServerOptions = null,
        Action<IMcpServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMcpServices(builder.Name);

        builder.ConfigureSchemaServices(
            services => services.AddMcpSchemaServices(configureServerOptions, configureServer));

        // TODO: MST we need to make sure that this directive is hidden in the introspection
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        builder.ConfigureOnRequestExecutorCreatedAsync(
            async (executor, cancellationToken) =>
            {
                var schema = executor.Schema;
                var storageObserver = schema.Services.GetRequiredService<McpStorageObserver>();
                await storageObserver.StartAsync(cancellationToken);
            });

        return builder;
    }

    public static IRequestExecutorBuilder AddMcpStorage(
        this IRequestExecutorBuilder builder,
        IMcpStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.ConfigureSchemaServices(s => s.AddSingleton(storage));

        return builder;
    }
}
