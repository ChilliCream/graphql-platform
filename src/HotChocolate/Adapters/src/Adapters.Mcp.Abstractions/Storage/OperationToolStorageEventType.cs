namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Defines the types of changes that can occur in operation tool storage.
/// </summary>
public enum OperationToolStorageEventType
{
    /// <summary>A tool was added to or updated in storage.</summary>
    Updated,

    /// <summary>A tool was removed from storage.</summary>
    Removed
}
