using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Serialization;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class PromptDefinition(string name)
{
    /// <summary>
    /// Gets the name of the prompt.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// Gets the optional human-readable title of the prompt for display purposes.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the optional human-readable description of the prompt.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional list of arguments to use for templating the prompt.
    /// </summary>
    public ImmutableArray<PromptArgumentDefinition>? Arguments { get; init; }

    /// <summary>
    /// Gets the optional icons for the prompt.
    /// </summary>
    public ImmutableArray<IconDefinition>? Icons { get; init; }

    /// <summary>
    /// Gets the messages that make up the prompt.
    /// </summary>
    public ImmutableArray<PromptMessageDefinition> Messages { get; init; } = [];

    public static PromptDefinition From(string name, McpPromptSettingsDto settings)
    {
        return new PromptDefinition(name)
        {
            Title = settings.Title,
            Description = settings.Description,
            Arguments = settings.Arguments?.Select(
                a => new PromptArgumentDefinition(a.Name)
                {
                    Title = a.Title,
                    Description = a.Description,
                    Required = a.Required
                }).ToImmutableArray(),
            Icons = settings.Icons?.Select(
                i => new IconDefinition(i.Source)
                {
                    MimeType = i.MimeType,
                    Sizes = i.Sizes,
                    Theme = i.Theme
                }).ToImmutableArray(),
            Messages = settings.Messages.Select(
                m =>
                {
                    return m.Content switch
                    {
                        McpPromptSettingsTextContentDto content => new PromptMessageDefinition(
                            MapRole(m.Role),
                            new TextContentBlockDefinition(content.Text)),
                        _ =>
                            throw new NotSupportedException(
                                $"Message content type '{m.Content.GetType().Name}' is not supported.")
                    };
                }).ToImmutableArray()
        };
    }

    private static RoleDefinition MapRole(string role)
    {
        return role switch
        {
            "user" => RoleDefinition.User,
            "assistant" => RoleDefinition.Assistant,
            _ => throw new NotSupportedException($"Role '{role}' is not supported.")
        };
    }
}
