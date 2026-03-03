using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

[McpServerToolType]
internal sealed class GetSchemaMembersStatisticsTool
{
    [McpServerTool(Name = "get_schema_members_statistics")]
    [Description(
        "Returns field-level usage statistics from the Nitro analytics API"
            + " for one or more GraphQL coordinates (e.g. 'User.email', 'Query.users')."
            + " Use this to determine whether a field is safe to remove, deprecate,"
            + " or modify.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        SchemaCache cache,
        SchemaStatisticsService statsService,
        NitroApiService apiService,
        [Description(
            "List of GraphQL coordinates in 'TypeName.fieldName' or" + " 'TypeName.fieldName(argName:)' format.")]
            string[] coordinates,
        [Description(
            "The stage name to query (e.g. 'production', 'preview')." + " Must match a stage configured for the API.")]
            string stage,
        [Description("API ID or path from .nitro/settings.json." + " If omitted, uses the default configured API.")]
            string? api = null,
        [Description("Start of the analysis window (ISO 8601)." + " Defaults to 30 days ago.")] string? from = null,
        [Description("End of the analysis window (ISO 8601)." + " Defaults to now.")] string? to = null,
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

        // 3. Validate input
        if (coordinates.Length == 0)
        {
            return FormatError("At least one coordinate is required.");
        }

        if (coordinates.Length > 50)
        {
            return FormatError("Maximum 50 coordinates per request.");
        }

        // 4. Parse time window
        DateTimeOffset fromDt;
        if (from is not null)
        {
            if (!DateTimeOffset.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDt))
            {
                return FormatError("Invalid 'from' date format. Expected ISO 8601 (e.g. '2025-01-01T00:00:00Z').");
            }
        }
        else
        {
            fromDt = DateTimeOffset.UtcNow.AddDays(-30);
        }

        DateTimeOffset toDt;
        if (to is not null)
        {
            if (!DateTimeOffset.TryParse(to, CultureInfo.InvariantCulture, DateTimeStyles.None, out toDt))
            {
                return FormatError("Invalid 'to' date format. Expected ISO 8601 (e.g. '2025-12-31T23:59:59Z').");
            }
        }
        else
        {
            toDt = DateTimeOffset.UtcNow;
        }

        // 5. Try to get schema index for deprecation enrichment (optional)
        var resolvedStage = stage;
        SchemaIndex? schemaIndex = null;
        try
        {
            schemaIndex = await cache.GetOrBuildAsync(
                resolveResult.ApiId,
                resolvedStage,
                _ => SchemaToolHelpers.FetchSchemaAsync(apiService, resolveResult.ApiId, resolvedStage, cancellationToken),
                cancellationToken);
        }
        catch
        {
            // Schema index is optional for statistics;
            // if it fails we still return stats without deprecationReason
        }

        // 6. Fetch statistics
        SchemaStatisticsResult result;
        try
        {
            result = await statsService.GetStatisticsAsync(
                apiService,
                schemaIndex,
                resolveResult.ApiId,
                stage,
                coordinates,
                fromDt,
                toDt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to fetch statistics: " + ex.Message);
        }

        return JsonSerializer.Serialize(result, SchemaStatisticsJsonContext.Default.SchemaStatisticsResult);
    }

    private static string FormatError(string message)
        => SchemaToolHelpers.FormatError(message, SchemaStatisticsJsonContext.Default.GetSchemaError);
}
