namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Defines the types of changes that can occur in prompt storage.
/// </summary>
public enum PromptStorageEventType
{
    /// <summary>A new prompt was added to storage.</summary>
    Added,

    /// <summary>A prompt was removed from storage.</summary>
    Removed
}
