using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class CreateApiTool
{
    [McpServerTool(Name = "create_api")]
    [Description(
        "Creates a new API in the current workspace. Returns the created API's "
            + "ID, name, path, and kind. Use this to register a new GraphQL service, "
            + "collection, or gateway before publishing schemas or creating clients.")]
    public static async Task<string> ExecuteAsync(
        ISessionService sessionService,
        ManagementService managementService,
        [Description("Name for the new API.")]
            string name,
        [Description(
            "Path in the workspace hierarchy (e.g. '/' or '/team/services'). "
                + "Defaults to '/'.")]
            string path = "/",
        [Description(
            "API kind: 'service' (single GraphQL endpoint), 'collection' "
                + "(group of related APIs), or 'gateway' (Fusion distributed gateway). "
                + "Defaults to 'service'.")]
            string kind = "service",
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
            var result = await managementService.CreateApiAsync(
                workspaceId, name, path, kind, cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.CreateApiResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to create API: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
