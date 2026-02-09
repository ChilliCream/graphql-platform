namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorManager
{
    private async Task WarmupExecutorAsync(
        IRequestExecutor executor,
        bool isInitialCreation,
        CancellationToken cancellationToken)
    {
        var warmupTasks = executor.Schema.Services
            .GetServices<IRequestExecutorWarmupTask>();

        if (!isInitialCreation)
        {
            warmupTasks = warmupTasks.Where(t => !t.ApplyOnlyOnStartup);
        }

        foreach (var warmupTask in warmupTasks)
        {
            await warmupTask.WarmupAsync(executor, cancellationToken).ConfigureAwait(false);
        }
    }
}
