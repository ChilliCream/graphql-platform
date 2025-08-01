using HotChocolate.ModelContextProtocol.Caching;
using HotChocolate.ModelContextProtocol.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class McpServerBuilderExtensions
{
    public static IMcpServerBuilder WithGraphQLTools(
        this IMcpServerBuilder builder,
        string? schemaName = null)
    {
        builder.Services.TryAddSingleton<IListToolsCache>(
            new MemoryListToolsCache(new MemoryCache(new MemoryCacheOptions())));

        builder
            .WithListToolsHandler(
                async (context, cancellationToken)
                    => await ListToolsHandler.HandleAsync(context, schemaName, cancellationToken))
            .WithCallToolHandler(
                async (context, cancellationToken)
                    => await CallToolHandler.HandleAsync(context, schemaName, cancellationToken));

        return builder;
    }
}
