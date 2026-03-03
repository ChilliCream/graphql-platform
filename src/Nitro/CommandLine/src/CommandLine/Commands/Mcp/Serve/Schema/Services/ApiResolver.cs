using ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

/// <summary>
/// Resolves an API ID from the provided parameter, the MCP context (--api-id flag),
/// or the project settings (.nitro/settings.json).
/// </summary>
internal sealed class ApiResolver
{
    private readonly NitroMcpContext _mcpContext;
    private readonly ProjectContext? _projectContext;

    public ApiResolver(NitroMcpContext mcpContext, ProjectContext? projectContext = null)
    {
        _mcpContext = mcpContext;
        _projectContext = projectContext;
    }

    public ApiResolveResult Resolve(string? apiParam)
    {
        // Step 1: explicit API ID parameter
        if (!string.IsNullOrWhiteSpace(apiParam))
        {
            return ApiResolveResult.Success(apiParam, _projectContext?.ActiveApi?.Name ?? apiParam);
        }

        // Step 2: pinned via --api-id flag (always available from NitroMcpContext)
        var pinnedApiId = _mcpContext.ApiId;
        if (!string.IsNullOrWhiteSpace(pinnedApiId))
        {
            var name = _projectContext?.ActiveApi?.Name ?? pinnedApiId;
            return ApiResolveResult.Success(pinnedApiId, name);
        }

        // Step 3: nothing resolvable
        return ApiResolveResult.Error(
            "Cannot resolve API. Provide 'api' parameter, "
                + "set --api-id at startup, "
                + "or create .nitro/settings.json with an 'apis' section.");
    }
}
