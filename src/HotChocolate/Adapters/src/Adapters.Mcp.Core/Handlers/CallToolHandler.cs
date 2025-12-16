using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class CallToolHandler
{
    public static async ValueTask<CallToolResult> HandleAsync(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken)
    {
        var services = context.Services!;
        var registry = services.GetRequiredService<McpFeatureRegistry>();

        if (!registry.TryGetTool(context.Params!.Name, out var tool))
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

        var requestExecutor = services.GetRequiredService<IRequestExecutor>();
        var arguments = context.Params?.Arguments ?? Enumerable.Empty<KeyValuePair<string, JsonElement>>();

        Dictionary<string, object?> variableValues = [];
        using var buffer = new PooledArrayWriter();
        var jsonValueParser = new JsonValueParser(buffer: buffer);

        foreach (var (name, value) in arguments)
        {
            variableValues.Add(name, jsonValueParser.Parse(value));
        }

        var rootServiceProvider = services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
        var httpContext = rootServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var requestBuilder = CreateRequestBuilder(httpContext);
        var request =
            requestBuilder
                .SetDocument(tool.DocumentNode)
                .SetVariableValues(variableValues)
                .Build();
        var result = await requestExecutor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        var operationResult = result.ExpectOperationResult();

        using var writer = new PooledArrayWriter();

        JsonResultFormatter.Indented.Format(operationResult, writer);
        var jsonOperationResult = Encoding.UTF8.GetString(writer.WrittenSpan);

        return new CallToolResult
        {
            // https://modelcontextprotocol.io/specification/2025-06-18/server/tools#structured-content
            // For backwards compatibility, a tool that returns structured content SHOULD
            // also return functionally equivalent unstructured content. (For example,
            // serialized JSON can be returned in a TextContent block.)
            Content = [new TextContentBlock { Text = jsonOperationResult }],
            StructuredContent = JsonNode.Parse(jsonOperationResult),
            IsError = operationResult.Errors?.Any()
        };
    }

    private static OperationRequestBuilder CreateRequestBuilder(HttpContext httpContext)
    {
        var requestBuilder = new OperationRequestBuilder();
        var userState = new UserState(httpContext.User);

        requestBuilder.Features.Set(userState);
        requestBuilder.Features.Set(httpContext);
        requestBuilder.Features.Set(httpContext.User);

        requestBuilder.TrySetServices(httpContext.RequestServices);
        requestBuilder.TryAddGlobalState(nameof(HttpContext), httpContext);
        requestBuilder.TryAddGlobalState(nameof(ClaimsPrincipal), userState.User);

        return requestBuilder;
    }
}
