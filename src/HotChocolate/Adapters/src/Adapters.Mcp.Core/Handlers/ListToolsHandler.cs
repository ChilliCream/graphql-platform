using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class ListToolsHandler
{
    public static ListToolsResult Handle(IServiceProvider schemaServices)
    {
        var registry = schemaServices.GetRequiredService<McpFeatureRegistry>();

        return new ListToolsResult
        {
            Tools = registry.GetTools().Select(t => t.Tool).ToList()
        };
    }
}
