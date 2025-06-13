namespace HotChocolate.Execution;

public interface IRequestExecutorProvider
{
    public ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default);
}
