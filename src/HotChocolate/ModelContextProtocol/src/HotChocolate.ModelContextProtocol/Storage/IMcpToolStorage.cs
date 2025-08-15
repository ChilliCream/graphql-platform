namespace HotChocolate.ModelContextProtocol.Storage;

public interface IMcpToolStorage : IObservable<McpToolStorageEventArgs>
{
    IAsyncEnumerable<McpTool> GetToolsAsync(CancellationToken cancellationToken = default);
}
