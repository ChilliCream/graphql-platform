namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorManager
{
    private bool _initialWarmupDone;

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        if (_initialWarmupDone)
        {
            return;
        }
        _initialWarmupDone = true;

        // we get the schema names for schemas that have warmup tasks.
        var schemasToWarmup = _warmupTasksBySchema.Keys;
        var tasks = new Task[schemasToWarmup.Length];

        for (var i = 0; i < schemasToWarmup.Length; i++)
        {
            // next we create an initial warmup for each schema
            tasks[i] = WarmupSchemaAsync(schemasToWarmup[i], cancellationToken);
        }

        // last we wait for all warmup tasks to complete.
        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task WarmupSchemaAsync(string schemaName, CancellationToken cancellationToken)
        {
            // the actual warmup tasks are executed inlined into the executor creation.
            await GetExecutorAsync(schemaName, cancellationToken).ConfigureAwait(false);
        }
    }
}
