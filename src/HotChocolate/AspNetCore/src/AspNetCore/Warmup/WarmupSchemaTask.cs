namespace HotChocolate.AspNetCore.Warmup;

internal sealed class WarmupSchemaTask
{
    private readonly Func<IRequestExecutor, CancellationToken, Task>? _warmup;

    public WarmupSchemaTask(
        string schemaName,
        bool keepWarm,
        Func<IRequestExecutor, CancellationToken, Task>? warmup = null)
    {
        _warmup = warmup;
        SchemaName = schemaName;
        KeepWarm = keepWarm;
    }

    public string SchemaName { get; }

    public bool KeepWarm { get; }

    public Task ExecuteAsync(IRequestExecutor executor, CancellationToken cancellationToken)
        => _warmup is not null
            ? _warmup.Invoke(executor, cancellationToken)
            : Task.CompletedTask;
}
