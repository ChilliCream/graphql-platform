using HotChocolate.ModelContextProtocol.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class McpServerBuilderExtensions
{
    public static IMcpServerBuilder WithGraphQLTools(
        this IMcpServerBuilder builder,
        string? schemaName = null)
    {
        builder
            .WithListToolsHandler(
                async (context, cancellationToken)
                    => await ListToolsHandler
                        .HandleAsync(context, schemaName, cancellationToken)
                        .ConfigureAwait(false))
            .WithCallToolHandler(
                async (context, cancellationToken)
                    => await CallToolHandler
                        .HandleAsync(context, schemaName, cancellationToken)
                        .ConfigureAwait(false));

        return builder;
    }
}
