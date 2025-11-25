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
            // TODO: See https://github.com/modelcontextprotocol/csharp-sdk/issues/1025.
            throw new McpProtocolException(
                string.Format(ReadResourceHandler_ResourceWithUriNotFound, context.Params.Uri),
                // TODO: See https://github.com/modelcontextprotocol/csharp-sdk/issues/863.
                (McpErrorCode)(-32002));
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
