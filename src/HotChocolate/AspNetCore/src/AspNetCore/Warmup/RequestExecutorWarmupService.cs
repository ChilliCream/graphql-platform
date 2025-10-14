using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Warmup;

internal sealed class RequestExecutorWarmupService(
    IRequestExecutorOptionsMonitor executorOptionsMonitor,
    IRequestExecutorProvider provider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var warmupTasks = new List<Task>();

        foreach (var schemaName in provider.SchemaNames)
        {
            var setup = await executorOptionsMonitor.GetAsync(schemaName, cancellationToken);
            var options = CreateSchemaOptions(setup);

            if (!options.LazyInitialization)
            {
                var warmupTask = WarmupAsync(schemaName, cancellationToken);
                warmupTasks.Add(warmupTask);
            }
        }

        await Task.WhenAll(warmupTasks).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WarmupAsync(string schemaName, CancellationToken cancellationToken)
    {
        await provider.GetExecutorAsync(schemaName, cancellationToken).ConfigureAwait(false);
    }

    private static SchemaOptions CreateSchemaOptions(RequestExecutorSetup setup)
    {
        var options = new SchemaOptions();

        foreach (var configure in setup.SchemaOptionModifiers)
        {
            configure(options);
        }

        return options;
    }
}
