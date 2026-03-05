using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class CreateClientTool
{
    [McpServerTool(Name = "create_client")]
    [Description(
        "Creates a new client for an API. Clients represent applications or "
            + "services that consume the GraphQL API and can be used for client "
            + "validation, persisted queries, and usage tracking.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        ManagementService managementService,
        [Description("Name for the new client.")]
            string name,
        [Description(
            "API ID (base64-encoded node ID). If omitted, uses the default "
                + "configured API from --api-id or .nitro/settings.json.")]
            string? apiId = null,
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
            var result = await managementService.CreateClientAsync(
                resolveResult.ApiId, name, cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.CreateClientResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to create client: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
