using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class ListPromptsHandler
{
    public static ListPromptsResult Handle(IServiceProvider schemaServices)
    {
        var registry = schemaServices.GetRequiredService<McpFeatureRegistry>();

        return new ListPromptsResult
        {
            Prompts = registry.GetPrompts().Select(t => t.Item1).ToList()
        };
    }
}
