using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class ListApisTool
{
    [McpServerTool(Name = "list_apis")]
    [Description(
        "Lists all APIs in the current workspace with pagination support. "
            + "Returns API IDs, names, paths, kinds, and pagination info. "
            + "Use this to discover available APIs before performing operations on them.")]
    public static async Task<string> ExecuteAsync(
        ISessionService sessionService,
        ManagementService managementService,
        [Description("Maximum number of APIs to return. Default: 50. Range: 1–100.")]
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

        var workspaceId = session.Workspace?.Id;
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return FormatError("No workspace selected. Run 'nitro workspace select' first.");
        }

        try
        {
            var clampedFirst = Math.Clamp(first, 1, 100);
            var result = await managementService.ListApisAsync(
                workspaceId, clampedFirst, after, cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.ListApisResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to list APIs: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
