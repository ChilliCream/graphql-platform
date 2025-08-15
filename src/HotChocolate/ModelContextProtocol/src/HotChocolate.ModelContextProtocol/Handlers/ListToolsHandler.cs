using HotChocolate.ModelContextProtocol.Registries;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol.Handlers;

internal static class ListToolsHandler
{
    public static ListToolsResult Handle(RequestContext<ListToolsRequestParams> context)
    {
        var registry = context.Services!.GetRequiredService<ToolRegistry>();

        return new ListToolsResult
        {
            Tools = registry.GetTools().Select(t => t.Tool).ToList()
        };
    }
}
