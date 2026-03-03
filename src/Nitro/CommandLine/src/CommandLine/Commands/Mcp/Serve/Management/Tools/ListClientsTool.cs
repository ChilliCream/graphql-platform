using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class ListClientsTool
{
    [McpServerTool(Name = "list_clients")]
    [Description(
        "Lists all clients for an API with pagination support. Returns client "
            + "IDs and names. Use this to see which applications consume the API.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        ManagementService managementService,
        [Description(
            "API ID (base64-encoded node ID). If omitted, uses the default "
                + "configured API from --api-id or .nitro/settings.json.")]
            string? apiId = null,
        [Description("Maximum number of clients to return. Default: 50. Range: 1–100.")]
            int first = 50,
        [Description("Pagination cursor from a previous response's pageInfo.endCursor.")]
            string? after = null,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionService.LoadSessionAsync(cancellationToken);
        if (session?.Tokens?.AccessToken is null)
        {
            return FormatError("Not authenticated. Run 'nitro login' first.");
        }

        var resolver = new ApiResolver(mcpContext);
        var resolveResult = resolver.Resolve(apiId);
        if (!resolveResult.IsSuccess)
        {
            return FormatError(resolveResult.ErrorMessage!);
        }

        try
        {
            var clampedFirst = Math.Clamp(first, 1, 100);
            var result = await managementService.ListClientsAsync(
                resolveResult.ApiId, clampedFirst, after, cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.ListClientsResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to list clients: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
