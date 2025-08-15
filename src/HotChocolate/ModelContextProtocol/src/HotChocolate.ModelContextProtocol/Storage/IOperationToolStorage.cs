namespace HotChocolate.ModelContextProtocol.Storage;

public interface IOperationToolStorage : IObservable<OperationToolStorageEventArgs>
{
    IAsyncEnumerable<OperationToolDefinition> GetToolsAsync(CancellationToken cancellationToken = default);
}
