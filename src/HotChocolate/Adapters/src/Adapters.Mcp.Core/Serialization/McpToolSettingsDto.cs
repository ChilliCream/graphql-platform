using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp.Serialization;

public sealed record McpToolSettingsDto
{
    public string? Title { get; set; }

    public List<McpToolSettingsIconDto>? Icons { get; set; }

    public McpToolSettingsAnnotationsDto? Annotations { get; set; }

    public McpToolSettingsMcpAppViewDto? View { get; set; }

    public List<McpAppViewVisibility>? Visibility { get; set; }
}

public sealed record McpToolSettingsIconDto
{
    public required Uri Source { get; set; }

    public string? MimeType { get; set; }

    public List<string>? Sizes { get; set; }

    public string? Theme { get; set; }
}

public sealed record McpToolSettingsAnnotationsDto
{
    public bool? DestructiveHint { get; set; }

    public bool? IdempotentHint { get; set; }

    public bool? OpenWorldHint { get; set; }
}

public sealed record McpToolSettingsMcpAppViewDto
{
    public McpToolSettingsCspDto? Csp { get; set; }

    public string? Domain { get; set; }

    public McpToolSettingsPermissionsDto? Permissions { get; set; }

    public bool? PrefersBorder { get; set; }
}

public class McpToolSettingsPermissionsDto
{
    public bool? Camera { get; set; }

    public bool? ClipboardWrite { get; set; }

    public bool? Geolocation { get; set; }

    public bool? Microphone { get; set; }
}

public sealed record McpToolSettingsCspDto
{
    public List<string>? BaseUriDomains { get; set; }

    public List<string>? ConnectDomains { get; set; }

    public List<string>? FrameDomains { get; set; }

    public List<string>? ResourceDomains { get; set; }
}
