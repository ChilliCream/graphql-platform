namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Defines the types of changes that can occur in operation tool storage.
/// </summary>
public enum OperationToolStorageEventType
{
    /// <summary>A new tool was added to storage.</summary>
    Added,

    /// <summary>A tool was removed from storage.</summary>
    Removed
}
