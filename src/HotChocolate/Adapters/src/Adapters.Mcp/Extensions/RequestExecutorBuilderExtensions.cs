#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
