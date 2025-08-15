namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Provides access to operation tool definitions with change notification support.
/// Implementations can retrieve tools from various sources (file system, database, etc.).
/// The Hot Chocolate MCP server will observe the <see cref="IOperationToolStorage"/>
/// and when changes are detected will phase in new tools, update tools or phase out tools
/// that have been removed from the storage.
/// </summary>
public interface IOperationToolStorage : IObservable<OperationToolStorageEventArgs>
{
    /// <summary>
    /// Retrieves all available operation tool definitions from the storage.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of all available tool definitions.</returns>
    ValueTask<IEnumerable<OperationToolDefinition>> GetToolsAsync(CancellationToken cancellationToken = default);
}
