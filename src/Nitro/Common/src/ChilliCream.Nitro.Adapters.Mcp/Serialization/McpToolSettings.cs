using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Storage;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public sealed record McpToolSettings
{
    public string? Title { get; init; }

    public ImmutableArray<McpToolSettingsIcon>? Icons { get; init; }

    public McpToolSettingsAnnotations? Annotations { get; init; }

    public McpToolSettingsMcpAppView? View { get; init; }

    public ImmutableArray<McpAppViewVisibility>? Visibility { get; init; }
}

public sealed record McpToolSettingsIcon
{
    public required Uri Source { get; init; }

    public string? MimeType { get; init; }

    public ImmutableArray<string>? Sizes { get; init; }

    public string? Theme { get; init; }
}

public sealed record McpToolSettingsAnnotations
{
    public bool? DestructiveHint { get; init; }

    public bool? IdempotentHint { get; init; }

    public bool? OpenWorldHint { get; init; }
}

public sealed record McpToolSettingsMcpAppView
{
    public McpToolSettingsCsp? Csp { get; init; }

    public string? Domain { get; init; }

    public McpToolSettingsPermissions? Permissions { get; init; }

    public bool? PrefersBorder { get; init; }
}

public class McpToolSettingsPermissions
{
    public bool? Camera { get; init; }

    public bool? ClipboardWrite { get; init; }

    public bool? Geolocation { get; init; }

    public bool? Microphone { get; init; }
}

public sealed record McpToolSettingsCsp
{
    public ImmutableArray<string>? BaseUriDomains { get; init; }

    public ImmutableArray<string>? ConnectDomains { get; init; }

    public ImmutableArray<string>? FrameDomains { get; init; }

    public ImmutableArray<string>? ResourceDomains { get; init; }
}
