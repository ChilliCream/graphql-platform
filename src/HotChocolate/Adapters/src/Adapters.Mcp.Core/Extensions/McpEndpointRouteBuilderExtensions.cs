using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Adapters.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Builder;

public static class McpEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapGraphQLMcp(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern = "/graphql/mcp",
        string? schemaName = null)
    {
        var manager = endpoints.ServiceProvider.GetService<McpManager>()
            ?? throw new InvalidOperationException("Call `AddMcp()` when configuring the GraphQL server.");

        TryResolveSchemaName(manager, ref schemaName);
        schemaName ??= ISchemaDefinition.DefaultName;

        var streamableHttpHandler = manager.Get(schemaName).HandlerProxy;

        var mcpGroup = endpoints.MapGroup(pattern);

        var streamableHttpGroup =
            mcpGroup
                .MapGroup("")
                .WithDisplayName(b => $"GraphQL MCP Streamable HTTP | {b.DisplayName}")
                .WithMetadata(
                    new ProducesResponseTypeMetadata(
                        StatusCodes.Status404NotFound,
                        typeof(JsonRpcError),
                        contentTypes: ["application/json"]));

        streamableHttpGroup
            .MapPost("", streamableHttpHandler.HandlePostRequestAsync)
            .WithMetadata(new AcceptsMetadata(["application/json"]))
            .WithMetadata(
                new ProducesResponseTypeMetadata(
                    StatusCodes.Status200OK,
                    contentTypes: ["text/event-stream"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status202Accepted));

        streamableHttpGroup
            .MapGet("", streamableHttpHandler.HandleGetRequestAsync)
            .WithMetadata(
                new ProducesResponseTypeMetadata(
                    StatusCodes.Status200OK,
                    contentTypes: ["text/event-stream"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status405MethodNotAllowed));

        streamableHttpGroup
            .MapDelete("", streamableHttpHandler.HandleDeleteRequestAsync)
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status405MethodNotAllowed));

        return mcpGroup;
    }

    private static void TryResolveSchemaName(McpManager manager, ref string? schemaName)
    {
        if (schemaName is null && manager.Names.Length == 1)
        {
            schemaName = manager.Names[0];
        }
    }
}
