using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public sealed record McpPromptSettings
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public List<McpPromptSettingsArgument>? Arguments { get; set; }

    public List<McpPromptSettingsIcon>? Icons { get; set; }

    public required List<McpPromptSettingsMessage> Messages { get; set; }
}

public sealed record McpPromptSettingsArgument
{
    public required string Name { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public bool Required { get; set; }
}

public sealed record McpPromptSettingsIcon
{
    public required Uri Source { get; set; }

    public string? MimeType { get; set; }

    public List<string>? Sizes { get; set; }

    public string? Theme { get; set; }
}

public sealed record McpPromptSettingsMessage
{
    public required string Role { get; set; }

    public required McpPromptSettingsMessageContent Content { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpPromptSettingsTextContent), "text")]
public abstract record McpPromptSettingsMessageContent;

public sealed record McpPromptSettingsTextContent : McpPromptSettingsMessageContent
{
    public required string Text { get; set; }
}
