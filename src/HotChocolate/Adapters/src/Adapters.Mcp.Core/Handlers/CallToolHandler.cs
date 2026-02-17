#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static class CallToolHandler
{
#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
        var rootServiceProvider = services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
        var httpContext = rootServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var requestBuilder = CreateRequestBuilder(httpContext);

        if (context.Params?.Arguments is { Count: > 0 } arguments)
        {
            requestBuilder.SetVariableValues(arguments);
        }

        var request = requestBuilder.SetDocument(tool.DocumentNode).Build();
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
            IsError = !operationResult.Errors.IsEmpty
        };
    }

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
