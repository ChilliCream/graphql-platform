using System.Collections.Immutable;

namespace HotChocolate.Adapters.Mcp.Serialization;

public sealed record McpToolSettingsDto
{
    public string? Title { get; set; }

    public List<McpToolSettingsIconDto>? Icons { get; set; }

    public McpToolSettingsAnnotationsDto? Annotations { get; set; }

    public McpToolSettingsOpenAiComponentDto? OpenAiComponent { get; set; }
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

public sealed record McpToolSettingsOpenAiComponentDto
{
    public string? Description { get; set; }

    public string? Domain { get; set; }

    public bool? PrefersBorder { get; set; }

    public string? ToolInvokingStatusText { get; set; }

    public string? ToolInvokedStatusText { get; set; }

    public bool? AllowToolCalls { get; set; }

    public McpToolSettingsCspDto? Csp { get; set; }
}

public sealed record McpToolSettingsCspDto
{
    public ImmutableArray<string>? ConnectDomains { get; set; }

    public ImmutableArray<string>? ResourceDomains { get; set; }
}
