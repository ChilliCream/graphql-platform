namespace HotChocolate.ModelContextProtocol.Storage;

public interface IOperationToolStorage : IObservable<OperationToolStorageEventArgs>
{
    ValueTask<IEnumerable<OperationToolDefinition>> GetToolsAsync(CancellationToken cancellationToken = default);
}
