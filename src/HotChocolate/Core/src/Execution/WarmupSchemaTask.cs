namespace HotChocolate.Execution;

internal sealed class WarmupSchemaTask(
    string schemaName,
    bool keepWarm,
    Func<IRequestExecutor, CancellationToken, Task>? warmup = null)
{
    public string SchemaName { get; } = schemaName;

    public bool KeepWarm { get; } = keepWarm;

    public Task ExecuteAsync(IRequestExecutor executor, CancellationToken cancellationToken)
        => warmup is not null
            ? warmup.Invoke(executor, cancellationToken)
            : Task.CompletedTask;
}
