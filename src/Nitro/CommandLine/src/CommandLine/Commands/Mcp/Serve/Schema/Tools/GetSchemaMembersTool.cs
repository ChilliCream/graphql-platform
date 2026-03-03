using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

[McpServerToolType]
internal sealed class GetSchemaMembersTool
{
    [McpServerTool(Name = "get_schema_members")]
    [Description(
        "Retrieve full details for one or more schema members by their GraphQL"
            + " coordinate (e.g. 'User.email', 'Query.user', 'OrderStatus')."
            + " Returns field types, arguments, directives, deprecation info,"
            + " and descriptions.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        SchemaCache cache,
        SchemaSearchService searcher,
        NitroApiService apiService,
        [Description(
            "List of GraphQL schema coordinates to retrieve."
                + " Examples: 'User', 'User.email', 'Query.user',"
                + " 'OrderStatus.PENDING'.")]
            string[] coordinates,
        [Description("API ID or path from .nitro/settings.json." + " If omitted, uses the default configured API.")]
            string? api = null,
        [Description("Deployment stage name." + " Defaults to the configured stage.")] string? stage = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Ensure authenticated
        var session = await sessionService.LoadSessionAsync(cancellationToken);
        if (session?.Tokens?.AccessToken is null)
        {
            return FormatError("Not authenticated. Run 'nitro login' first.");
        }

        // 2. Resolve API
        var resolver = new ApiResolver(mcpContext);
        var resolveResult = resolver.Resolve(api);
        if (!resolveResult.IsSuccess)
        {
            return FormatError(resolveResult.ErrorMessage!);
        }

        var resolvedStage = stage ?? mcpContext.Stage;

        // 3. Validate input
        if (coordinates.Length == 0)
        {
            return FormatError("At least one coordinate is required.");
        }

        if (coordinates.Length > 50)
        {
            return FormatError("Maximum 50 coordinates per request.");
        }

        // 4. Get or build index
        SchemaIndex index;
        try
        {
            index = await cache.GetOrBuildAsync(
                resolveResult.ApiId,
                resolvedStage,
                _ => SchemaToolHelpers.FetchSchemaAsync(apiService, resolveResult.ApiId, resolvedStage, cancellationToken),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to load schema: " + ex.Message);
        }

        // 5. Lookup members
        var result = searcher.GetMembers(index, coordinates);

        return JsonSerializer.Serialize(result, SchemaSearchJsonContext.Default.GetMembersResult);
    }

    private static string FormatError(string message)
        => SchemaToolHelpers.FormatError(message, SchemaSearchJsonContext.Default.GetSchemaError);
}
