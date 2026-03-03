using System.ComponentModel;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;

[McpServerToolType]
internal sealed class UpdateApiSettingsTool
{
    [McpServerTool(Name = "update_api_settings")]
    [Description(
        "Updates schema registry settings for an API. Configure whether "
            + "dangerous schema changes are treated as breaking, or whether "
            + "breaking changes are allowed at all. Returns the updated API name on success.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        ManagementService managementService,
        [Description(
            "API ID (base64-encoded node ID). If omitted, uses the default "
                + "configured API from --api-id or .nitro/settings.json.")]
            string? apiId = null,
        [Description("When true, dangerous schema changes (e.g. adding a required argument) are treated as breaking.")]
            bool? treatDangerousAsBreaking = null,
        [Description("When true, breaking schema changes are allowed to be published.")]
            bool? allowBreakingSchemaChanges = null,
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
            var result = await managementService.UpdateApiSettingsAsync(
                resolveResult.ApiId,
                treatDangerousAsBreaking,
                allowBreakingSchemaChanges,
                cancellationToken);

            return JsonSerializer.Serialize(result, ManagementJsonContext.Default.UpdateApiSettingsResult);
        }
        catch (Exception ex)
        {
            return FormatError("Failed to update API settings: " + ex.Message);
        }
    }

    private static string FormatError(string message)
    {
        var error = new ManagementError { Error = message };
        return JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
    }
}
