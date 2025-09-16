namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Event arguments for operation tool storage changes.
/// </summary>
/// <param name="Name">The name of the tool that changed.</param>
/// <param name="Type">The type of change that occurred.</param>
/// <param name="ToolDefinition">The tool definition. Required for Added/Modified, null for Removed.</param>
public record OperationToolStorageEventArgs(
    string Name,
    OperationToolStorageEventType Type,
    OperationToolDefinition? ToolDefinition = null);
