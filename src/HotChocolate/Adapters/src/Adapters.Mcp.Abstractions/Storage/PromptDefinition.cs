using System.Collections.Immutable;

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
}
