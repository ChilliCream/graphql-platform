namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class PromptArgumentDefinition(string name)
{
    /// <summary>
    /// Gets the name of the prompt argument.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// Gets the optional human-readable title of the prompt argument for display purposes.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the optional human-readable description of the prompt argument.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the prompt argument is required.
    /// </summary>
    public bool? Required { get; init; }
}
