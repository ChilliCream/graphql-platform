namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Provides access to MCP feature definitions with change notification support.
/// Implementations can retrieve definitions from various sources (file system, database, etc.).
/// The Hot Chocolate MCP server will observe the <see cref="IMcpStorage"/>
/// and when changes are detected will phase in new definitions, update definitions, or phase out
/// definitions that have been removed from the storage.
/// </summary>
public interface IMcpStorage
    : IObservable<OperationToolStorageEventArgs>
    , IObservable<PromptStorageEventArgs>
{
    /// <summary>
    /// Retrieves all available operation tool definitions from the storage.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of all available operation tool definitions.</returns>
    ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available prompt definitions from the storage.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of all available prompt definitions.</returns>
    ValueTask<IEnumerable<PromptDefinition>> GetPromptDefinitionsAsync(
        CancellationToken cancellationToken = default);
}
