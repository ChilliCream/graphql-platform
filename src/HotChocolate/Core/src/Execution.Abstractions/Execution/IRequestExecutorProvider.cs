namespace HotChocolate.Execution;

public interface IRequestExecutorProvider
{
    public ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = null,
        CancellationToken cancellationToken = default);
}
