using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.Adapters.Mcp.Serialization;

public sealed record McpPromptSettings
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public ImmutableArray<McpPromptSettingsArgument>? Arguments { get; init; }

    public ImmutableArray<McpPromptSettingsIcon>? Icons { get; init; }

    public required ImmutableArray<McpPromptSettingsMessage> Messages { get; init; }
}

public sealed record McpPromptSettingsArgument
{
    public required string Name { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public bool Required { get; init; }
}

public sealed record McpPromptSettingsIcon
{
    public required Uri Source { get; init; }

    public string? MimeType { get; init; }

    public ImmutableArray<string>? Sizes { get; init; }

    public string? Theme { get; init; }
}

public sealed record McpPromptSettingsMessage
{
    public required string Role { get; init; }

    public required McpPromptSettingsMessageContent Content { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpPromptSettingsTextContent), "text")]
public abstract record McpPromptSettingsMessageContent;

public sealed record McpPromptSettingsTextContent : McpPromptSettingsMessageContent
{
    public required string Text { get; init; }
}
