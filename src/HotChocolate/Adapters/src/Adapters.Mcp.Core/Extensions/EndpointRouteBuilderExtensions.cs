using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapGraphQLMcp(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern = "/graphql/mcp",
        string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        var manager = endpoints.ServiceProvider.GetService<McpManager>()
            ?? throw new InvalidOperationException(
                "You must call AddMcp(). Unable to find required services. Call "
                + "builder.Services.AddGraphQL().AddMcp() in application startup code.");

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

        if (!streamableHttpHandler.HttpServerTransportOptions.Stateless)
        {
            // The GET endpoint is not mapped in Stateless mode since there's no way to send
            // unsolicited messages. Resuming streams via GET is currently not supported in
            // Stateless mode.
            streamableHttpGroup
                .MapGet("", streamableHttpHandler.HandleGetRequestAsync)
                .WithMetadata(
                    new ProducesResponseTypeMetadata(
                        StatusCodes.Status200OK,
                        contentTypes: ["text/event-stream"]));

            // The DELETE endpoint is not mapped in Stateless mode since there is no server-side
            // state for the DELETE to clean up.
            streamableHttpGroup.MapDelete("", streamableHttpHandler.HandleDeleteRequestAsync);
        }

        return mcpGroup;
    }
}
