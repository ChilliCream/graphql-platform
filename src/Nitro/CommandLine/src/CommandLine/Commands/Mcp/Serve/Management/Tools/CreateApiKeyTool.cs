using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class CreateApiKeyTool
{
    [McpServerTool(Name = "create_api_key")]
    [Description(
        "Creates a new API key in the workspace. The secret is returned only "
            + "once in the response and cannot be retrieved later. Optionally scope "
            + "the key to a specific API or deployment stage.")]
    public static async Task<string> ExecuteAsync(
        ISessionService sessionService,
        ManagementService managementService,
        [Description("Name for the new API key.")]
            string name,
        [Description("Scope the key to a specific API ID. If omitted, the key is workspace-wide.")]
            string? apiId = null,
        [Description("Restrict the key to a specific deployment stage name (e.g. 'production').")]
            string? stageName = null,
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
            var result = await managementService.CreateApiKeyAsync(
                workspaceId, name, apiId, stageName, cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.CreateApiKeyResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to create API key: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
