using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal class ExecutorWarmupService(IRequestExecutorResolver executorResolver, IEnumerable<WarmupSchemaTask> tasks)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var warmupTasks = tasks.GroupBy(t => t.SchemaName)
            .Select(g => executorResolver.GetRequestExecutorAsync(g.Key, cancellationToken).ConfigureAwait(false));

        foreach (var warmupTask in warmupTasks)
        {
            await warmupTask;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
