namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Describes a message returned as part of a prompt.
/// </summary>
public sealed class PromptMessageDefinition(RoleDefinition role, IContentBlockDefinition contentBlock)
{
    /// <summary>
    /// Gets the role of the prompt message.
    /// </summary>
    public RoleDefinition Role { get; init; } = role;

    /// <summary>
    /// Gets the content of the prompt message.
    /// </summary>
    public IContentBlockDefinition Content { get; init; } = contentBlock;
}
