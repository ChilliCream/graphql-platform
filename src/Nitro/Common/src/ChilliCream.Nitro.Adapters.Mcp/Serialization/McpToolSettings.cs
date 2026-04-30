using HotChocolate.Adapters.Mcp.Storage;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public sealed record McpToolSettings
{
    public string? Title { get; set; }

    public List<McpToolSettingsIcon>? Icons { get; set; }

    public McpToolSettingsAnnotations? Annotations { get; set; }

    public McpToolSettingsMcpAppView? View { get; set; }

    public List<McpAppViewVisibility>? Visibility { get; set; }
}

public sealed record McpToolSettingsIcon
{
    public required Uri Source { get; set; }

    public string? MimeType { get; set; }

    public List<string>? Sizes { get; set; }

    public string? Theme { get; set; }
}

public sealed record McpToolSettingsAnnotations
{
    public bool? DestructiveHint { get; set; }

    public bool? IdempotentHint { get; set; }

    public bool? OpenWorldHint { get; set; }
}

public sealed record McpToolSettingsMcpAppView
{
    public McpToolSettingsCsp? Csp { get; set; }

    public string? Domain { get; set; }

    public McpToolSettingsPermissions? Permissions { get; set; }

    public bool? PrefersBorder { get; set; }
}

public class McpToolSettingsPermissions
{
    public bool? Camera { get; set; }

    public bool? ClipboardWrite { get; set; }

    public bool? Geolocation { get; set; }

    public bool? Microphone { get; set; }
}

public sealed record McpToolSettingsCsp
{
    public List<string>? BaseUriDomains { get; set; }

    public List<string>? ConnectDomains { get; set; }

    public List<string>? FrameDomains { get; set; }

    public List<string>? ResourceDomains { get; set; }
}
