using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

[McpServerToolType]
internal sealed class SearchSchemaMemberTool
{
    [McpServerTool(Name = "search_schema_member")]
    [Description(
        "Search the GraphQL schema for types, fields, arguments, enum values,"
            + " and other members by name or description keyword. Returns ranked"
            + " results with schema coordinates, types, descriptions, and paths"
            + " from root operations to each member.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        SchemaCache cache,
        SchemaSearchService searcher,
        NitroApiService apiService,
        [Description(
            "Search text. Matched against member names and descriptions."
                + " Supports multiple words (all words must match for top results).")]
            string query,
        [Description("API ID or path from .nitro/settings.json." + " If omitted, uses the default configured API.")]
            string? api = null,
        [Description(
            "Deployment stage name (e.g. 'development', 'staging', 'production')."
                + " Defaults to the configured stage.")]
            string? stage = null,
        [Description("Filter results to a specific member kind.")] SchemaIndexMemberKind? kind = null,
        [Description("Maximum number of results to return. Default: 20. Max: 100.")] int limit = 20,
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

        // 3. Get or build index
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

        // 4. Search
        var clampedLimit = Math.Clamp(limit, 1, 100);
        var result = searcher.Search(index, query, kind, clampedLimit);

        return JsonSerializer.Serialize(result, SchemaSearchJsonContext.Default.SearchResult);
    }

    private static string FormatError(string message)
        => SchemaToolHelpers.FormatError(message, SchemaSearchJsonContext.Default.GetSchemaError);
}
