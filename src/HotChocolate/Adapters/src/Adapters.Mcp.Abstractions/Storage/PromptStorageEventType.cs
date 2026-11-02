namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Defines the types of changes that can occur in prompt storage.
/// </summary>
public enum PromptStorageEventType
{
    /// <summary>A prompt was added to or updated in storage.</summary>
    Updated,

    /// <summary>A prompt was removed from storage.</summary>
    Removed
}
