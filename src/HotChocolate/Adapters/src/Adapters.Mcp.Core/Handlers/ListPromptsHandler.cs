using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class ListPromptsHandler
{
    public static ListPromptsResult Handle(RequestContext<ListPromptsRequestParams> context)
    {
        var registry = context.Services!.GetRequiredService<McpFeatureRegistry>();

        return new ListPromptsResult
        {
            Prompts = registry.GetPrompts().Select(t => t.Item1).ToList()
        };
    }
}
