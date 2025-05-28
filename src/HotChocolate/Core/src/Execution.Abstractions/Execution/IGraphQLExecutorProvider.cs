namespace HotChocolate.Execution;

public interface IGraphQLExecutorProvider
{
    public ValueTask<IGraphQLExecutor> GetExecutorAsync(
        string schemaName,
        CancellationToken cancellationToken = default);
}
