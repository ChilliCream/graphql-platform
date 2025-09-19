namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Defines the types of changes that can occur in operation tool storage.
/// </summary>
public enum OperationToolStorageEventType
{
    /// <summary>A new tool was added to storage.</summary>
    Added,

    /// <summary>An existing tool was modified in storage.</summary>
    Modified,

    /// <summary>A tool was removed from storage.</summary>
    Removed
}
