using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.ModelContextProtocol.Factories;
using HotChocolate.ModelContextProtocol.Handlers;
using HotChocolate.ModelContextProtocol.Proxies;
using HotChocolate.ModelContextProtocol.Registries;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.ModelContextProtocol.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(this IRequestExecutorBuilder builder)
    {
        builder.Services
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
                    .TryAddSingleton
                        <IMcpOperationDocumentStorage, InMemoryMcpOperationDocumentStorage>();

                services
                    .AddSingleton(
                        static sp => sp
                            .GetRootServiceProvider()
                            .GetRequiredService<IHostApplicationLifetime>())
                    .AddSingleton(
                        static sp => sp
                            .GetRootServiceProvider().GetRequiredService<ILoggerFactory>())
                    .AddSingleton<GraphQLMcpToolRegistry>();

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

        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        builder.ConfigureOnRequestExecutorCreatedAsync(
            async (executor, cancellationToken) =>
            {
                var schema = executor.Schema;
                var storage = schema.Services.GetRequiredService<IMcpOperationDocumentStorage>();
                var registry = schema.Services.GetRequiredService<GraphQLMcpToolRegistry>();
                var factory = new GraphQLMcpToolFactory(schema);

                var toolDocuments =
                    await storage.GetToolDocumentsAsync(cancellationToken).ConfigureAwait(false);

                registry.Clear();

                foreach (var (name, document) in toolDocuments)
                {
                    registry.Add(factory.CreateTool(name, document));
                }
            });

        return builder;
    }

    public static IRequestExecutorBuilder AddMcpOperationDocumentStorage(
        this IRequestExecutorBuilder builder,
        IMcpOperationDocumentStorage storage)
    {
        builder.ConfigureSchemaServices(s => s.AddSingleton(storage));
        return builder;
    }
}
