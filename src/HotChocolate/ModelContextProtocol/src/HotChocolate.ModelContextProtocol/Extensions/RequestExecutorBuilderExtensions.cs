using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.ModelContextProtocol.Handlers;
using HotChocolate.ModelContextProtocol.Proxies;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.ModelContextProtocol.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(this IRequestExecutorBuilder builder)
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
            services =>
            {
                services
                    .TryAddSingleton<IOperationToolStorage, InMemoryOperationToolStorage>();

                services
                    .TryAddSingleton(
                        static sp => new OperationToolFactory(
                            sp.GetRequiredService<ISchemaDefinition>()));

                services
                    .TryAddSingleton(
                        static sp => new ToolStorageObserver(
                            sp.GetRequiredService<ToolRegistry>(),
                            sp.GetRequiredService<OperationToolFactory>(),
                            sp.GetRequiredService<StreamableHttpHandler>(),
                            sp.GetRequiredService<IOperationToolStorage>()));

                services
                    .AddSingleton(
                        static sp => sp
                            .GetRootServiceProvider()
                            .GetRequiredService<IHostApplicationLifetime>())
                    .AddSingleton(
                        static sp => sp
                            .GetRootServiceProvider()
                            .GetRequiredService<ILoggerFactory>())
                    .AddSingleton<ToolRegistry>();

                services
                    .AddMcpServer(o => o.Capabilities?.Tools?.ListChanged = true)
                    .WithHttpTransport()
                    .WithListToolsHandler(
                        (context, _) => ValueTask.FromResult(ListToolsHandler.Handle(context)))
                    .WithCallToolHandler(
                        async (context, cancellationToken)
                            => await CallToolHandler
                                .HandleAsync(context, cancellationToken)
                                .ConfigureAwait(false));
            });

        // TODO: MST we need to make sure that this directive is hidden in the introspection
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        builder.ConfigureOnRequestExecutorCreatedAsync(
            async (executor, cancellationToken) =>
            {
                var schema = executor.Schema;
                var storageObserver = schema.Services.GetRequiredService<ToolStorageObserver>();
                await storageObserver.StartAsync(cancellationToken);
            });

        return builder;
    }

    public static IRequestExecutorBuilder AddMcpToolStorageStorage(
        this IRequestExecutorBuilder builder,
        IOperationToolStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.ConfigureSchemaServices(s => s.AddSingleton(storage));

        return builder;
    }
}
