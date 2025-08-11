using HotChocolate.Execution.Configuration;
using HotChocolate.ModelContextProtocol.Factories;
using HotChocolate.ModelContextProtocol.Registries;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.ModelContextProtocol.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMcp(this IRequestExecutorBuilder builder)
    {
        builder.ConfigureSchemaServices(
            services =>
            {
                services
                    .TryAddSingleton
                        <IMcpOperationDocumentStorage, InMemoryMcpOperationDocumentStorage>();

                services.AddSingleton<GraphQLMcpToolRegistry>();
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
