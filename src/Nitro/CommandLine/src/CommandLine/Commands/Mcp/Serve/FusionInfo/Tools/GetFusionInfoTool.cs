using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Tools;

[McpServerToolType]
internal sealed class GetFusionInfoTool
{
    [McpServerTool(Name = "get_fusion_info")]
    [Description(
        "Downloads and decomposes the Fusion gateway configuration for a "
            + "deployment stage. Returns subgraph metadata including names, "
            + "endpoint URLs, root-type entry points, schema coordinate counts, "
            + "and file system paths to extracted schema files. Use this to "
            + "understand the structure of a distributed GraphQL gateway before "
            + "making schema changes or queries across subgraphs.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        FusionInfoService fusionInfoService,
        [Description(
            "API ID (base64-encoded node ID). If omitted, uses the "
                + "default configured API from --api-id or .nitro/settings.json.")]
            string? api = null,
        [Description(
            "Deployment stage name (e.g. 'development', 'staging', "
                + "'production'). Defaults to the configured stage.")]
            string? stage = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Ensure authenticated
        var session = await sessionService.LoadSessionAsync(cancellationToken);
        if (session?.Tokens?.AccessToken is null)
        {
            return FormatError("Not authenticated. Run 'nitro login' first.");
        }

        // 2. Resolve API ID
        var resolver = new ApiResolver(mcpContext);
        var resolveResult = resolver.Resolve(api);
        if (!resolveResult.IsSuccess)
        {
            return FormatError(resolveResult.ErrorMessage!);
        }

        var resolvedStage = stage ?? mcpContext.Stage;

        // 3. Fetch and decompose the fusion configuration
        try
        {
            var result = await fusionInfoService.GetFusionInfoAsync(
                resolveResult.ApiId,
                resolvedStage,
                cancellationToken);

            return JsonSerializer.Serialize(result, FusionInfoJsonContext.Default.FusionInfoResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to load fusion info: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new FusionInfoError { Error = message };
        return JsonSerializer.Serialize(error, FusionInfoJsonContext.Default.FusionInfoError);
    }
}
