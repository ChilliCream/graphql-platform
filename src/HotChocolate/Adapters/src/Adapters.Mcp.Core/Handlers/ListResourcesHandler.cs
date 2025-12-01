using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class ListResourcesHandler
{
    public static ListResourcesResult Handle(RequestContext<ListResourcesRequestParams> context)
    {
        var toolRegistry = context.Services!.GetRequiredService<ToolRegistry>();

        var openAiComponentResources =
            toolRegistry
                .GetTools()
                .Select(t => t.OpenAiComponentResource)
                .OfType<Resource>()
                .ToList();

        return new ListResourcesResult
        {
            Resources = openAiComponentResources
        };
    }
}
