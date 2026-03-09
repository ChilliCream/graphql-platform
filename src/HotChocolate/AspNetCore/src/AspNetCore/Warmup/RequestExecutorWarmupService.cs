using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Warmup;

internal sealed class RequestExecutorWarmupService(
    IOptionsMonitor<RequestExecutorSetup> optionsMonitor,
    IRequestExecutorProvider provider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var warmupTasks = new List<Task>();

        foreach (var schemaName in provider.SchemaNames)
        {
            var setup = optionsMonitor.Get(schemaName);
            var options = setup.CreateSchemaOptions();

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
}
