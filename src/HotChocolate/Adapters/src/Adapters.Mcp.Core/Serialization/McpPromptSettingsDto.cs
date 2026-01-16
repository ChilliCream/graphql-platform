using System.Text.Json.Serialization;

namespace HotChocolate.Adapters.Mcp.Serialization;

public sealed record McpPromptSettingsDto
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public List<McpPromptSettingsArgumentDto>? Arguments { get; set; }

    public List<McpPromptSettingsIconDto>? Icons { get; set; }

    public required List<McpPromptSettingsMessageDto> Messages { get; set; }
}

public sealed record McpPromptSettingsArgumentDto
{
    public required string Name { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public bool Required { get; set; }
}

public sealed record McpPromptSettingsIconDto
{
    public required Uri Source { get; set; }

    public string? MimeType { get; set; }

    public List<string>? Sizes { get; set; }

    public string? Theme { get; set; }
}

public sealed record McpPromptSettingsMessageDto
{
    public required string Role { get; set; }

    public required McpPromptSettingsMessageContentDto Content { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpPromptSettingsTextContentDto), "text")]
public abstract record McpPromptSettingsMessageContentDto;

public sealed record McpPromptSettingsTextContentDto : McpPromptSettingsMessageContentDto
{
    public required string Text { get; set; }
}
