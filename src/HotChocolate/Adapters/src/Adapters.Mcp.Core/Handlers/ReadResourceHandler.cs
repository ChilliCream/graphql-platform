using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class ReadResourceHandler
{
    public static ReadResourceResult Handle(RequestContext<ReadResourceRequestParams> context)
    {
        var toolRegistry = context.Services!.GetRequiredService<ToolRegistry>();

        if (!toolRegistry.TryGetToolByOpenAiComponentResourceUri(context.Params!.Uri, out var tool))
        {
            throw new McpException(
                string.Format(ReadResourceHandler_ResourceWithUriNotFound, context.Params.Uri));
        }

        return new ReadResourceResult
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = tool.OpenAiComponentResource!.Uri,
                    MimeType = tool.OpenAiComponentResource!.MimeType,
                    Text = tool.OpenAiComponentHtml!,
                    Meta = tool.OpenAiComponentResource!.Meta
                }
            ]
        };
    }
}
