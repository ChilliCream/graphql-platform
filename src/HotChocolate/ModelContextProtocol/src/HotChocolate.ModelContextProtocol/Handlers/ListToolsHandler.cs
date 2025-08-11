using HotChocolate.Execution;
using HotChocolate.ModelContextProtocol.Registries;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol.Handlers;

internal static class ListToolsHandler
{
    public static async ValueTask<ListToolsResult> HandleAsync(
        RequestContext<ListToolsRequestParams> context,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var executorProvider = context.Services!.GetRequiredService<IRequestExecutorProvider>();
        var requestExecutor =
            await executorProvider
                .GetExecutorAsync(schemaName, cancellationToken)
                .ConfigureAwait(false);
        var registry = requestExecutor.Schema.Services.GetRequiredService<GraphQLMcpToolRegistry>();

        return new ListToolsResult
        {
            Tools = registry.GetTools().Values.Select(t => t.McpTool).ToList()
        };
    }
}
