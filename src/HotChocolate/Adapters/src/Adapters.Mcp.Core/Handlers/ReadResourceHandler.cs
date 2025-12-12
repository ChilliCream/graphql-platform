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
        var registry = context.Services!.GetRequiredService<McpFeatureRegistry>();

        if (!registry.TryGetToolByOpenAiComponentResourceUri(context.Params!.Uri, out var tool))
        {
            throw new McpProtocolException(
                string.Format(ReadResourceHandler_ResourceNotFound, context.Params.Uri),
                McpErrorCode.ResourceNotFound)
            {
                Data =
                {
                    { "uri", context.Params!.Uri }
                }
            };
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
