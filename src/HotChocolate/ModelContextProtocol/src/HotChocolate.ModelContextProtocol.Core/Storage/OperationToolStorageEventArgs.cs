using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Event arguments for operation tool storage changes.
/// </summary>
/// <param name="Name">The name of the tool that changed.</param>
/// <param name="Type">The type of change that occurred.</param>
/// <param name="Document">The GraphQL document for the tool. Required for Added/Modified, null for Removed.</param>
public record OperationToolStorageEventArgs(
    string Name,
    OperationToolStorageEventType Type,
    DocumentNode? Document = null);
