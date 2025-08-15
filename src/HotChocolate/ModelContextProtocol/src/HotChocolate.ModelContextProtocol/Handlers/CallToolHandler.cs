using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Registries;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Handlers;

internal static class CallToolHandler
{
    public static async ValueTask<CallToolResult> HandleAsync(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken)
    {
        var registry = context.Services!.GetRequiredService<GraphQLMcpToolRegistry>();

        if (!registry.TryGetTool(context.Params!.Name, out var graphQLMcpTool))
        {
            return new CallToolResult
            {
                Content =
                [
                    new TextContentBlock
                    {
                        Text = string.Format(CallToolHandler_ToolNotFound, context.Params.Name)
                    }
                ],
                IsError = true
            };
        }

        var requestExecutor = context.Services!.GetRequiredService<IRequestExecutor>();
        var arguments =
            context.Params?.Arguments ?? Enumerable.Empty<KeyValuePair<string, JsonElement>>();

        Dictionary<string, object?> variableValues = [];
        using var buffer = new PooledArrayWriter();
        var jsonValueParser = new JsonValueParser(buffer: buffer);

        foreach (var (name, value) in arguments)
        {
            variableValues.Add(name, jsonValueParser.Parse(value));
        }

        var result =
            await requestExecutor.ExecuteAsync(
                b => b
                    .SetDocument(graphQLMcpTool.Operation.Document)
                    .SetVariableValues(variableValues),
                cancellationToken)
                .ConfigureAwait(false);

        var operationResult = result.ExpectOperationResult();

        return new CallToolResult
        {
            // https://modelcontextprotocol.io/specification/2025-06-18/server/tools#structured-content
            // For backwards compatibility, a tool that returns structured content SHOULD
            // also return functionally equivalent unstructured content. (For example,
            // serialized JSON can be returned in a TextContent block.)
            Content = [new TextContentBlock { Text = operationResult.ToJson() }],
            StructuredContent = JsonNode.Parse(operationResult.ToJson()),
            IsError = operationResult.Errors?.Any()
        };
    }
}
