using HotChocolate.ModelContextProtocol.Registries;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol.Handlers;

internal static class ListToolsHandler
{
    public static ListToolsResult Handle(RequestContext<ListToolsRequestParams> context)
    {
        var registry = context.Services!.GetRequiredService<GraphQLMcpToolRegistry>();

        return new ListToolsResult
        {
            Tools = registry.GetTools().Values.Select(t => t.McpTool).ToList()
        };
    }
}
