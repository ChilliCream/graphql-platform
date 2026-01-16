namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Event arguments for prompt storage changes.
/// </summary>
/// <param name="Name">The name of the prompt that changed.</param>
/// <param name="Type">The type of change that occurred.</param>
/// <param name="PromptDefinition">The prompt definition. Required for Added/Modified, null for Removed.</param>
public record PromptStorageEventArgs(
    string Name,
    PromptStorageEventType Type,
    PromptDefinition? PromptDefinition = null);
